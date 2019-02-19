﻿using System;

namespace ExClient.Search
{
    public sealed class AdvancedSearchResult : CategorySearchResult
    {
        internal static AdvancedSearchResult Search(string keyword, Category category, AdvancedSearchOptions advancedSearch)
        {
            return new AdvancedSearchResult(keyword, category, advancedSearch);
        }

        private string getQueryString() => new AdvancedSearchOptions(advSearchData).ToSearchQuery();

        private AdvancedSearchResult(string keyword, Category category, AdvancedSearchOptions advancedSearch)
            : base(keyword, category)
        {
            if (advancedSearch != null)
            {
                advSearchData = advancedSearch.Data;
            }

            SearchUri = advSearchData == default
                ? base.SearchUri
                : new Uri(base.SearchUri.OriginalString + getQueryString());
        }

        private readonly ulong advSearchData;

        public AdvancedSearchOptions AdvancedSearch => new AdvancedSearchOptions(advSearchData);

        public override Uri SearchUri { get; }
    }
}
