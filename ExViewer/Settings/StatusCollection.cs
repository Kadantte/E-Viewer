﻿using ApplicationDataManager.Settings;

namespace ExViewer.Settings
{
    class StatusCollection : ApplicationSettingCollection
    {
        public static StatusCollection Current
        {
            get;
        } = new StatusCollection();

        private StatusCollection()
            : base("Status") { }

        public bool FullScreenInImagePage
        {
            get => GetLocal(false);
            set => SetLocal(value);
        }

        public string TorrentFolderToken
        {
            get => GetLocal(default(string));
            set => SetLocal(value);
        }
    }
}
