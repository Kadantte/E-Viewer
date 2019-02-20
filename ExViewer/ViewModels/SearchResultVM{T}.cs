﻿using ExClient.Galleries;
using ExClient.Search;
using ExViewer.Database;
using ExViewer.Views;
using Opportunity.MvvmUniverse;
using Opportunity.MvvmUniverse.Collections;
using Opportunity.MvvmUniverse.Commands;
using Opportunity.MvvmUniverse.Views;
using System;
using Windows.Foundation;
using static System.Runtime.InteropServices.WindowsRuntime.AsyncInfo;

namespace ExViewer.ViewModels
{
    public abstract class SearchResultVM<T> : ViewModelBase
        where T : SearchResult
    {
        protected SearchResultVM(T searchResult)
        {
            this.searchResult = searchResult;
            SetQueryWithSearchResult();
        }

        public string SearchQuery => this.SearchResult.SearchUri.ToString();

        public Command<Gallery> Open => Commands.GetOrAdd(() =>
            Command<Gallery>.Create(async (sender, g) =>
            {
                var that = (SearchResultVM<T>)sender.Tag;
                that.SelectedGallery = g;
                GalleryVM.GetVM(g);
                await RootControl.RootController.Navigator.NavigateAsync(typeof(GalleryPage), g.Id);
            }, (sender, g) => g != null));

        public Command<string> Search => Commands.Get<Command<string>>();

        private T searchResult;
        public T SearchResult
        {
            get => this.searchResult;
            protected set => Set(ref this.searchResult, value);
        }

        public virtual void SetQueryWithSearchResult()
        {
            this.Keyword = this.SearchResult.Keyword;
        }

        private Gallery selectedGallery;
        public Gallery SelectedGallery { get => this.selectedGallery; protected set => Set(ref this.selectedGallery, value); }

        private string keyword;
        public string Keyword { get => this.keyword; set => Set(ref this.keyword, value); }
    }
}