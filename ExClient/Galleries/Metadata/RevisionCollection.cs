﻿using ExClient.Api;
using HtmlAgilityPack;
using Opportunity.Helpers.Universal.AsyncHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace ExClient.Galleries.Metadata
{
    public readonly struct RevisionInfo
    {
        internal RevisionInfo(GalleryInfo gallery, DateTimeOffset updatedTime)
        {
            Gallery = gallery;
            UpdatedTime = updatedTime;
        }

        public GalleryInfo Gallery { get; }
        public DateTimeOffset UpdatedTime { get; }
    }

    public class RevisionCollection
    {
        internal RevisionCollection(Gallery owner, HtmlDocument doc)
        {
            Owner = owner;
            var gdd = doc.GetElementbyId("gdd");
            var parentNode = gdd.FirstChild.ChildNodes[1].Descendants("a").FirstOrDefault();
            if (parentNode != null)
            {
                ParentInfo = GalleryInfo.Parse(parentNode.GetAttribute("href", default(Uri)));
            }

            var descendantsNode = doc.GetElementbyId("gnd");
            if (descendantsNode != null)
            {
                var count = descendantsNode.ChildNodes.Count / 3;
                var descendants = new RevisionInfo[count];
                for (var i = 0; i < descendants.Length; i++)
                {
                    var aNode = descendantsNode.ChildNodes[i * 3 + 1];
                    var textNode = descendantsNode.ChildNodes[i * 3 + 2];
                    var link = aNode.GetAttribute("href", default(Uri));
                    var dto = DateTimeOffset.ParseExact(textNode.GetInnerText(), "', added' yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AllowWhiteSpaces);
                    descendants[i] = new RevisionInfo(GalleryInfo.Parse(link), dto);
                }
                DescendantsInfo = descendants;
            }
            else
            {
                DescendantsInfo = Array.Empty<RevisionInfo>();
            }
        }

        internal Gallery Owner { get; }
        public GalleryInfo? ParentInfo { get; }
        public IReadOnlyList<RevisionInfo> DescendantsInfo { get; }

        public IAsyncOperation<Gallery> FetchParentAsync()
        {
            if (!(this.ParentInfo is GalleryInfo i))
            {
                return AsyncOperation<Gallery>.CreateCompleted(null);
            }

            return i.FetchGalleryAsync();
        }

        public IAsyncOperation<Gallery> FetchLatestRevisionAsync()
        {
            if (DescendantsInfo.Count == 0)
            {
                return AsyncOperation<Gallery>.CreateCompleted(Owner);
            }

            return DescendantsInfo.Last().Gallery.FetchGalleryAsync();
        }
    }
}
