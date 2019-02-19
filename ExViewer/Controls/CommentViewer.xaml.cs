﻿using ExClient.Galleries.Commenting;
using ExViewer.Views;
using Opportunity.Helpers.ObjectModel;
using Opportunity.MvvmUniverse;
using Opportunity.MvvmUniverse.Commands;
using Opportunity.MvvmUniverse.Commands.ReentrancyHandlers;
using Opportunity.MvvmUniverse.Views;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ExViewer.Controls
{
    public sealed partial class CommentViewer : UserControl
    {
        private class CommentVM : ViewModelBase
        {
            private readonly static AsyncCommand<Comment> edit, reply;

            static CommentVM()
            {
                edit = AsyncCommand<Comment>.Create(async (s, c) =>
                {
                    var dialog = ThreadLocalSingleton.GetOrCreate<EditCommentDialog>();
                    dialog.EditableComment = c;
                    await dialog.ShowAsync();
                }, (s, c) => c != null && c.CanEdit);
                edit.ReentrancyHandler = ReentrancyHandler.LastQueued<Comment>();
                reply = AsyncCommand<Comment>.Create(async (s, c) =>
                {
                    var dialog = ThreadLocalSingleton.GetOrCreate<ReplyCommentDialog>();
                    dialog.ReplyingComment = c;
                    await dialog.ShowAsync();
                }, (s, c) => c != null && !c.CanEdit);
                reply.ReentrancyHandler = ReentrancyHandler.LastQueued<Comment>();
            }

            public CommentVM()
            {
                Translate.Tag = this;
                VoteUp.Tag = this;
                VoteDown.Tag = this;
                VoteWithdraw.Tag = this;
            }

            private Comment comment;
            public Comment Comment
            {
                get => comment;
                set
                {
                    if (Set(ref comment, value))
                    {
                        TranslatedContent = null;
                    }
                }
            }

            public bool CanVoteUp(CommentStatus status) => status == CommentStatus.Votable || status == CommentStatus.VotedDown;
            public bool CanVoteDown(CommentStatus status) => status == CommentStatus.Votable || status == CommentStatus.VotedUp;
            public bool CanVoteWithdraw(CommentStatus status) => status == CommentStatus.VotedUp || status == CommentStatus.VotedDown;

            private HtmlAgilityPack.HtmlNode translated;
            public HtmlAgilityPack.HtmlNode TranslatedContent
            {
                get => translated;
                private set
                {
                    if (Set(ref translated, value))
                    {
                        Translate.OnCanExecuteChanged();
                    }
                }
            }

            public AsyncCommand<Comment> Translate { get; } = AsyncCommand<Comment>.Create(async (s, c) =>
            {
                var r = await c.TranslateAsync(Settings.SettingCollection.Current.CommentTranslationCode);
                ((CommentVM)s.Tag).TranslatedContent = r;
            }, (s, c) => c != null && ((CommentVM)s.Tag).translated is null);

            public AsyncCommand<Comment> VoteUp { get; }
                = AsyncCommand<Comment>.Create((s, c) => c.VoteAsync(ExClient.Api.VoteState.Up), (s, c) => c != null && ((CommentVM)s.Tag).CanVoteUp(c.Status));

            public AsyncCommand<Comment> VoteDown { get; }
                = AsyncCommand<Comment>.Create((s, c) => c.VoteAsync(ExClient.Api.VoteState.Down), (s, c) => c != null && ((CommentVM)s.Tag).CanVoteDown(c.Status));

            public AsyncCommand<Comment> VoteWithdraw { get; }
                = AsyncCommand<Comment>.Create((s, c) => c.VoteAsync(ExClient.Api.VoteState.Default), (s, c) => c != null && ((CommentVM)s.Tag).CanVoteWithdraw(c.Status));

            public AsyncCommand<Comment> Edit => edit;

            public AsyncCommand<Comment> Reply => reply;
        }

        public CommentViewer()
        {
            InitializeComponent();
            VM.Translate.Executed += (s, e) =>
            {
                var ex = e.Exception;
                e.Handled = true;
                if (ex != null)
                {
                    RootControl.RootController.SendToast(ex, typeof(GalleryPage));
                }
            };
            FocusEngaged += CommentViewer_FocusEngaged;
            FocusDisengaged += CommentViewer_FocusDisengaged;
        }

        private void CommentViewer_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        {
            ElementSoundPlayer.Play(ElementSoundKind.GoBack);
        }
        private void CommentViewer_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            ElementSoundPlayer.Play(ElementSoundKind.Invoke);
        }

        private readonly CommentVM VM = new CommentVM();

        public Comment Comment
        {
            get => (Comment)GetValue(CommentProperty);
            set => SetValue(CommentProperty, value);
        }

        // Using a DependencyProperty as the backing store for Comment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentProperty =
            DependencyProperty.Register("Comment", typeof(Comment), typeof(CommentViewer), new PropertyMetadata(null, CommentPropertyChanged));

        private static void CommentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (CommentViewer)d;
            sender.VM.Comment = (Comment)e.NewValue;
        }

        protected override void OnDisconnectVisualChildren()
        {
            ClearValue(CommentProperty);
            base.OnDisconnectVisualChildren();
        }

        private static double toOpacity(HtmlAgilityPack.HtmlNode val)
        {
            if (val is null)
            {
                return 1;
            }

            return 0.7;
        }
    }
}
