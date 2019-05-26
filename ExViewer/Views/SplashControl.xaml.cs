﻿using ExClient;
using ExClient.Api;
using ExClient.Forums;
using ExClient.Services;
using ExViewer.Controls;
using ExViewer.Settings;
using ExViewer.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ExViewer.Views
{
    public sealed partial class SplashControl : UserControl
    {
        private SplashScreen splashScreen;

        public SplashControl(SplashScreen splashScreen)
        {
            InitializeComponent();
            if (Opportunity.Helpers.Universal.ApiInfo.IsMobile)
            {
                gd_Foreground.VerticalAlignment = VerticalAlignment.Stretch;
            }
            BannerProvider.Provider.GetBannerAsync().Completed = (s, e)
                => Dispatcher.Begin(() => loadBanner(s.GetResults()));
            loadApplication();
            this.splashScreen = splashScreen;
        }

        private async void loadBanner(StorageFile banner)
        {
            if (banner is null)
            {
                ((BitmapImage)img_pic.Source).UriSource = BannerProvider.Provider.DefaultBanner;
                return;
            }
            using (var stream = await banner.OpenReadAsync())
            {
                await ((BitmapImage)img_pic.Source).SetSourceAsync(stream);
            }
        }

        private void splash_Loading(FrameworkElement sender, object args)
        {
            Themes.ThemeExtention.SetTitleBar();
            Themes.ThemeExtention.SetDefaltImage();
        }

        private void ShowPic_Completed(object sender, object e)
        {
            FindName(nameof(pr));
        }

        private void img_pic_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            loadBanner(null);
            // After the default image loaded, img_pic_ImageOpened() will be called.
        }

        private void img_pic_ImageOpened(object sender, RoutedEventArgs e)
        {
            loadEffect();
        }

        private void splash_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Opportunity.Helpers.Universal.ApiInfo.IsMobile)
            {
                var s = e.NewSize;
                if (s.Height >= s.Width)
                {
                    gd_Foreground.Height = double.NaN;
                    img_pic.Height = s.Width / 620 * 136;
                }
                else
                {
                    gd_Foreground.Height = s.Height / 2.5;
                    img_pic.Height = s.Height / 2.5 / 300 * 136;
                }
            }
            else
            {
                var l = splashScreen.ImageLocation;
                if (Opportunity.Helpers.Universal.ApiInfo.IsXbox)
                {
                    // Xbox has a 200% scale
                    l = new Windows.Foundation.Rect(l.X / 2, l.Y / 2, l.Width / 2, l.Height / 2);
                }
                gd_Foreground.Margin = new Thickness(0, l.Top, 0, 0);
                gd_Foreground.Height = l.Height;
                img_pic.Height = l.Height / 300 * 136;
            }

        }

        private RootControl rootControl;

        private async void goToContent()
        {
            if (SettingCollection.Current.NeedVerify)
            {
                await Helpers.VerificationManager.VerifyAsync();
            }

            ccHided.Content = null;
            Window.Current.Content = rootControl;
            rootControl = null;
            afterActions();
        }

        private void setLoadingFinished()
        {
            if (goToContentEnabled)
            {
                goToContent();
            }
            else
            {
                loadingFinished = true;
            }
        }

        public void EnableGoToContent()
        {
            if (loadingFinished)
            {
                goToContent();
            }
            else
            {
                goToContentEnabled = true;
            }
        }

        private bool loadingFinished, goToContentEnabled;

        private bool effectLoaded, applicationLoaded;

        private bool oobe;

        private async void loadEffect()
        {
            await Dispatcher.YieldIdle();
            Window.Current.Activate();
            ShowPic.Begin();
            if (applicationLoaded)
            {
                setLoadingFinished();
            }
            else
            {
                effectLoaded = true;
            }
        }

        private async void loadApplication()
        {
            var loadingTask = Task.Run(async () =>
            {
                var client = Client.Current;
                if (!client.NeedLogOn)
                {
                    SettingCollection.Current.Apply();
                    var initSearchTask = WatchedVM.InitAsync();
                    var waitTime = 0;
                    while (waitTime < 7000)
                    {
                        await Task.Delay(250);
                        waitTime += 250;
                        if (initSearchTask.Status != Windows.Foundation.AsyncStatus.Started)
                        {
                            initSearchTask.Close();
                            break;
                        }
                    }
                }
                ExClient.HentaiVerse.HentaiVerseInfo.MonsterEncountered += (s, e) =>
                {
                    if (SettingCollection.Current.OpenHVOnMonsterEncountered)
                    {
                        CoreApplication.MainView.Dispatcher.Begin(async () =>
                        {
                            await Windows.System.Launcher.LaunchUriAsync(e.Uri);
                        });
                    }
                };
            });
            await Dispatcher.YieldIdle();
            rootControl = new RootControl();
            FindName(nameof(ccHided));
            ccHided.Content = rootControl;
            await loadingTask;
            if (Client.Current.NeedLogOn)
            {
                oobe = true;
                await RootControl.RootController.RequestLogOn();
            }
            if (effectLoaded)
            {
                setLoadingFinished();
            }
            else
            {
                applicationLoaded = true;
            }
        }

        private async void afterActions()
        {
            try
            {
                if (!oobe)
                    await Client.Current.RefreshCookiesAsync();
            }
            catch (Exception)
            {
                //Ignore exceptions here.
            }
            try
            {
                var ver = await VersionChecker.CheckAsync();
                if (ver is VersionChecker.GitHubRelease v)
                {
                    var dialog = new UpdateDialog(v);
                    await dialog.ShowAsync();
                }
            }
            catch (Exception)
            {
                //Ignore exceptions here.
            }
            try
            {
                await ExClient.HentaiVerse.HentaiVerseInfo.FetchAsync();
            }
            catch (Exception)
            {
                //Ignore exceptions here.
            }
            try
            {
                if (await EhTagTranslatorClient.Client.NeedUpdateAsync())
                {
                    AboutControl.UpdateETT.Execute();
                }
            }
            catch (Exception)
            {
                RootControl.RootController.SendToast(Strings.Resources.Database.EhTagTranslatorClient.Update.Failed, null);
            }
            if (DateTimeOffset.Now - EhTagClient.Client.LastUpdate > new TimeSpan(7, 0, 0, 0))
            {
                AboutControl.UpdateEhWiki.Execute();
            }
            if (DateTimeOffset.Now - BannerProvider.Provider.LastUpdate > new TimeSpan(7, 0, 0, 0))
            {
                try
                {
                    await BannerProvider.Provider.FetchBanners();
                }
                catch (Exception)
                {
                    //Ignore exceptions here.
                }
            }
        }
    }
}
