﻿using System;

namespace ExClient.Status
{
    public readonly struct ToplistItem : IEquatable<ToplistItem>
    {
        internal ToplistItem(int rank, ToplistName name)
        {
            Rank = rank;
            Name = name;
        }

        public int Rank { get; }
        public ToplistName Name { get; }

        public bool Equals(ToplistItem other)
            => Name == other.Name
            && Rank == other.Rank;

        public override bool Equals(object obj) => obj is ToplistItem t && this.Equals(t);

        public override int GetHashCode() => Rank << 16 ^ (int)Name;

        public static bool operator ==(in ToplistItem left, in ToplistItem right) => left.Equals(right);
        public static bool operator !=(in ToplistItem left, in ToplistItem right) => !left.Equals(right);
    }

    public enum ToplistName
    {
        GalleriesAllTime = 11,
        GalleriesPastYear = 12,
        GalleriesPastMonth = 13,
        GalleriesYesterday = 15,

        UploaderAllTime = 21,
        UploaderPastYear = 22,
        UploaderPastMonth = 23,
        UploaderYesterday = 25,

        TaggingAllTime = 31,
        TaggingPastYear = 32,
        TaggingPastMonth = 33,
        TaggingYesterday = 35,

        HentaiAtHomeAllTime = 41,
        HentaiAtHomePastYear = 42,
        HentaiAtHomePastMonth = 43,
        HentaiAtHomeYesterday = 45,

        EHTrackerAllTime = 51,
        EHTrackerPastYear = 52,
        EHTrackerPastMonth = 53,
        EHTrackerYesterday = 55,

        CleanupAllTime = 61,
        CleanupPastYear = 62,
        CleanupPastMonth = 63,
        CleanupYesterday = 65,

        RatingAndReviewingAllTime = 71,
        RatingAndReviewingPastYear = 72,
        RatingAndReviewingPastMonth = 73,
        RatingAndReviewingYesterday = 75,
    }

    public static class ToplistNameExtension
    {
        public static Uri Uri(this ToplistName topList)
        {
            if (!topList.IsDefined())
                return new Uri(Internal.DomainProvider.Eh.RootUri, $"toplist.php"); ;
            return new Uri(Internal.DomainProvider.Eh.RootUri, $"toplist.php?tl={(int)topList}");
        }

        public static string ToFriendlyNameString(this ToplistName topList)
        {
            return topList.ToFriendlyNameString(LocalizedStrings.Toplist.GetValue);
        }
    }
}
