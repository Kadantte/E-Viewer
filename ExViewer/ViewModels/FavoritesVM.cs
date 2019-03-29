﻿using ExClient;
using ExClient.Search;
using ExViewer.Database;
using ExViewer.Views;
using Opportunity.MvvmUniverse.Collections;
using Opportunity.MvvmUniverse.Commands;
using System;

namespace ExViewer.ViewModels
{
    public sealed class FavoritesVM : SearchResultVM<FavoritesSearchResult>
    {
        private static AutoFillCacheStorage<string, FavoritesVM> Cache = AutoFillCacheStorage.Create((string query) =>
        {
            var search = default(FavoritesSearchResult);
            var record = new HistoryRecord
            {
                Type = HistoryRecordType.Favorites,
            };
            if (string.IsNullOrEmpty(query))
            {
                record.Uri = FavoritesSearchResult.SearchBaseUri;
                search = Client.Current.Favorites.All.Search("");
            }
            else
            {
                var uri = new Uri(query);

                var handle = ExClient.Launch.UriLauncher.HandleAsync(uri);
                search = (FavoritesSearchResult)((ExClient.Launch.SearchLaunchResult)handle.Result).Data;
            }
            var vm = new FavoritesVM(search);
            HistoryDb.Add(new HistoryRecord
            {
                Type = HistoryRecordType.Favorites,
                Uri = vm.SearchResult.SearchUri,
                Title = vm.Keyword,
            });
            return vm;
        }, 10);

        public static FavoritesVM GetVM(string query) => Cache.GetOrCreateAsync(query ?? string.Empty).GetResults();

        public static FavoritesVM GetVM(FavoritesSearchResult searchResult)
        {
            var vm = new FavoritesVM(searchResult ?? throw new ArgumentNullException(nameof(searchResult)));
            var query = vm.SearchQuery;
            HistoryDb.Add(new HistoryRecord
            {
                Type = HistoryRecordType.Favorites,
                Uri = vm.SearchResult.SearchUri,
                Title = vm.Keyword,
            });
            Cache[query] = vm;
            return vm;
        }

        private FavoritesVM(FavoritesSearchResult searchResult)
            : base(searchResult)
        {
            Commands.Add(nameof(Search), Command<string>.Create(async (sender, queryText) =>
            {
                var that = (FavoritesVM)sender.Tag;
                var cat = that.category ?? Client.Current.Favorites.All;
                var search = cat.Search(queryText);
                var vm = GetVM(search);
                await RootControl.RootController.Navigator.NavigateAsync(typeof(FavoritesPage), vm.SearchQuery);
            }));
        }

        public override void SetQueryWithSearchResult()
        {
            base.SetQueryWithSearchResult();
            Category = SearchResult.Category;
        }

        private FavoriteCategory category;
        public FavoriteCategory Category
        {
            get => category;
            set => Set(ref category, value);
        }
    }
}
