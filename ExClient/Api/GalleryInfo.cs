﻿using ExClient.Galleries;
using ExClient.Internal;
using ExClient.Launch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using static System.Runtime.InteropServices.WindowsRuntime.AsyncInfo;

namespace ExClient.Api
{
    [JsonConverter(typeof(GalleryInfoConverter))]
    public readonly struct GalleryInfo : IEquatable<GalleryInfo>
    {
        private class GalleryInfoConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(GalleryInfo) == objectType;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    return null;
                }

                long gid = 0;
                ulong token = 0;
                reader.Read();
                do
                {
                    switch (reader.Value.ToString())
                    {
                    case "gid":
                        gid = reader.ReadAsInt32().GetValueOrDefault();
                        break;
                    case "token":
                        token = reader.ReadAsString().ToToken();
                        break;
                    default:
                        break;
                    }
                    reader.Read();
                } while (reader.TokenType != JsonToken.EndObject);

                return new GalleryInfo(gid, token);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var v = (GalleryInfo)value;
                writer.WriteStartArray();
                writer.WriteValue(v.ID);
                writer.WriteValue(v.Token.ToTokenString());
                writer.WriteEndArray();
            }
        }

        public static IAsyncOperation<IReadOnlyList<GalleryInfo>> FetchGalleryInfoListAsync(IEnumerable<ImageInfo> pageList)
        {
            return Run<IReadOnlyList<GalleryInfo>>(async token =>
            {
                var res = await new GalleryTokenRequest(pageList).GetResponseAsync(token);
                return res.TokenList;
            });
        }

        private static readonly string[] galleryPaths = new[] { "g", "mpv", };

        internal static bool TryParseGallery(UriHandlerData data, out GalleryInfo info)
        {
            info = default;
            if (data.Paths.Count < 3)
                return false;
            if (!Array.Exists(galleryPaths, s => data.Path0.Equals(s, StringComparison.OrdinalIgnoreCase)))
                return false;
            if (!long.TryParse(data.Paths[1], out var gId))
                return false;

            info = new GalleryInfo(gId, data.Paths[2].ToToken());
            return true;
        }

        private static readonly string[] popups = new[] { "gallerytorrents.php", "gallerypopups.php", "stats.php", "archiver.php", };

        internal static bool TryParseGalleryPopup(UriHandlerData data, out GalleryInfo info, out GalleryLaunchStatus type)
        {
            info = default;
            type = default;
            if (data.Paths.Count < 1)
                return false;
            if (!Array.Exists(popups, s => data.Path0.Equals(s, StringComparison.OrdinalIgnoreCase)))
                return false;
            if (!long.TryParse(data.Queries.GetString("gid"), out var gId))
                return false;

            var token = data.Queries.GetString("t") + data.Queries.GetString("token");
            if (token.IsNullOrWhiteSpace())
                return false;
            info = new GalleryInfo(gId, token.ToToken());
            type = GalleryLaunchStatus.Default;
            switch (data.Path0)
            {
            case "gallerytorrents.php":
                type = GalleryLaunchStatus.Torrent;
                break;
            case "stats.php":
                type = GalleryLaunchStatus.Stats;
                break;
            case "archiver.php":
                type = GalleryLaunchStatus.Archive;
                break;
            default:
                switch (data.Queries.GetString("act"))
                {
                case "addfav":
                    type = GalleryLaunchStatus.Favorite;
                    break;
                case "expunge":
                    type = GalleryLaunchStatus.Expunge;
                    break;
                case "rename":
                    type = GalleryLaunchStatus.Rename;
                    break;
                }
                break;
            }
            return true;
        }

        public static bool TryParse(Uri uri, out GalleryInfo info)
        {
            var data = new UriHandlerData(uri);
            if (TryParseGallery(data, out info))
                return true;
            if (TryParseGalleryPopup(data, out info, out _))
                return true;

            info = default;
            return false;
        }

        public static GalleryInfo Parse(Uri uri)
        {
            if (TryParse(uri, out var r))
                return r;
            throw new FormatException();
        }

        public GalleryInfo(long id, ulong token)
        {
            ID = id;
            Token = token;
        }

        public IAsyncOperation<Gallery> FetchGalleryAsync()
        {
            var galleryInfo = new[] { this };
            return Run(async token =>
            {
                var d = await Gallery.FetchGalleriesAsync(galleryInfo);
                return d[0];
            });
        }

        public long ID { get; }

        public ulong Token { get; }

        public Uri Uri => new Uri(Client.Current.Uris.RootUri, $"g/{ID.ToString()}/{Token.ToTokenString()}/");

        public override int GetHashCode()
            => ID.GetHashCode() * 19 ^ Token.GetHashCode();

        public override bool Equals(object obj) => obj is GalleryInfo info && Equals(info);

        public bool Equals(GalleryInfo other)
            => ID == other.ID
            && Token == other.Token;

        public static bool operator ==(in GalleryInfo left, in GalleryInfo right) => left.Equals(right);

        public static bool operator !=(in GalleryInfo left, in GalleryInfo right) => !left.Equals(right);

        public static implicit operator GalleryInfo(Gallery gallery)
        {
            if (gallery is null)
                return default;
            return new GalleryInfo(gallery.Id, gallery.Token);
        }
    }
}