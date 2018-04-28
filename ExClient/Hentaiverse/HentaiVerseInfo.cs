﻿using HtmlAgilityPack;
using Opportunity.Helpers.Universal.AsyncHelpers;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.Web.Http;

namespace ExClient.HentaiVerse
{
    public static class HentaiVerseInfo
    {
        public static Uri RootUri { get; } = new Uri("http://hentaiverse.org/");

        public static Uri LogOnUri => new Uri(RootUri, $"login.php?ipb_member_id={Client.Current.UserID}&ipb_pass_hash={Client.Current.PassHash}");

        public static event EventHandler<MonsterEncounteredEventArgs> MonsterEncountered;

        public static bool IsEnabled { get; set; } = false;

        private static Uri newsUri = new Uri("https://e-hentai.org/news.php");

        public static IAsyncActionWithProgress<HttpProgress> FetchAsync()
            => Client.Current.HttpClient.GetDocumentAsync(newsUri).ContinueWith(a => { });

        internal static void AnalyzePage(HtmlDocument doc)
        {
            var eventPane = doc.GetElementbyId("eventpane");
            if (eventPane is null)
                return;
            var div1 = eventPane.Element("div");
            if (div1 is null)
                return;
            var a = eventPane.Descendants("a").FirstOrDefault();
            if (a != null && MonsterEncountered != null)
            {
                var uri = a.GetAttribute("href", default(Uri));
                MonsterEncountered?.Invoke(Client.Current, new MonsterEncounteredEventArgs(uri));
            }
        }
    }
}
