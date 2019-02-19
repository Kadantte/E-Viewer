﻿using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

#pragma warning disable CS0618 // 类型或成员已过时

namespace ExViewer.Controls
{
    public class PullToRefreshIndicator : Control
    {
        public PullToRefreshIndicator()
        {
            DefaultStyleKey = typeof(PullToRefreshIndicator);
            Loading += PullToRefreshIndicator_Loading;
            Unloaded += PullToRefreshIndicator_Unloaded;
        }

        private void PullToRefreshIndicator_Loading(FrameworkElement sender, object args)
        {
            var s = (PullToRefreshIndicator)sender;
            var pv = s.Ancestors<PullToRefreshListView>().First();
            s.parent = pv;
        }

        private PullToRefreshListView p;

        private PullToRefreshListView parent
        {
            get => p;
            set
            {
                if (p != null)
                {
                    p.PullProgressChanged -= Parent_PullProgressChanged;
                }

                p = value;
                if (p != null)
                {
                    p.PullProgressChanged += Parent_PullProgressChanged;
                }

                ClearValue(PullProgressProperty);
            }
        }

        private void PullToRefreshIndicator_Unloaded(object sender, RoutedEventArgs e)
        {
            ((PullToRefreshIndicator)sender).parent = null;
        }

        private void Parent_PullProgressChanged(object sender, RefreshProgressEventArgs e)
        {
            PullProgress = e.PullProgress;
        }

        public double PullProgress
        {
            get => (double)GetValue(PullProgressProperty);
            set => SetValue(PullProgressProperty, value);
        }

        // Using a DependencyProperty as the backing store for PullProgress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PullProgressProperty =
            DependencyProperty.Register("PullProgress", typeof(double), typeof(PullToRefreshIndicator), new PropertyMetadata(0d, PullProgressChanged));

        private static void PullProgressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (PullToRefreshIndicator)sender;
            if ((double)e.NewValue == 1.0)
            {
                VisualStateManager.GoToState(s, "Actived", true);
            }
            else
            {
                VisualStateManager.GoToState(s, "Normal", true);
            }
        }
    }
}

#pragma warning restore CS0618 // 类型或成员已过时