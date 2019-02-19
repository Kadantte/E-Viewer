﻿using ExClient;
using Opportunity.MvvmUniverse;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ExViewer.Controls
{
    public sealed partial class CategorySelector : UserControl
    {
        public CategorySelector()
        {
            InitializeComponent();
            filter = new List<FilterRecord>()
            {
                new FilterRecord(Category.Doujinshi,true),
                new FilterRecord(Category.Manga, true),
                new FilterRecord(Category.ArtistCG, true),
                new FilterRecord(Category.GameCG, true),
                new FilterRecord(Category.Western, true),
                new FilterRecord(Category.NonH, true),
                new FilterRecord(Category.ImageSet, true),
                new FilterRecord(Category.Cosplay, true),
                new FilterRecord(Category.AsianPorn, true),
                new FilterRecord(Category.Misc, true)
            };
            foreach (var item in filter)
            {
                item.PropertyChanged += filterItem_PropertyChanged;
            }
        }

        private void filterItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var category = Category.Unspecified;
            foreach (var item in filter)
            {
                if (item.IsChecked)
                {
                    category |= item.Category;
                }
            }
            SelectedCategory = category;
        }

        public Category SelectedCategory
        {
            get => (Category)GetValue(SelectedCategoryProperty);
            set => SetValue(SelectedCategoryProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedCategory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCategoryProperty =
            DependencyProperty.Register("SelectedCategory", typeof(Category), typeof(CategorySelector), new PropertyMetadata(Category.All, selectedCategoryPropertyChangedCallback));

        private static void selectedCategoryPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var s = (CategorySelector)d;
            var oldValue = (Category)e.OldValue;
            var newValue = (Category)e.NewValue;
            if (oldValue == newValue)
            {
                return;
            }

            foreach (var item in s.filter)
            {
                item.IsChecked = newValue.HasFlag(item.Category);
            }
        }

        private List<FilterRecord> filter;
    }

    internal class FilterRecord : ObservableObject
    {
        public FilterRecord(Category category, bool isChecked)
        {
            Category = category;
            IsChecked = isChecked;
        }

        public Category Category
        {
            get;
        }

        private bool isChecked;

        public bool IsChecked
        {
            get => isChecked;
            set => Set(ref isChecked, value);
        }
    }
}
