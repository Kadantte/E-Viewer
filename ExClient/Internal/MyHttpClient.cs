﻿using ExClient.HentaiVerse;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using static System.Runtime.InteropServices.WindowsRuntime.AsyncInfo;
using IHttpAsyncOperation = Windows.Foundation.IAsyncOperationWithProgress<Windows.Web.Http.HttpResponseMessage, Windows.Web.Http.HttpProgress>;

namespace ExClient.Internal
{
    /*
     * 由于使用了自定义 Filter 后发生异常会丢失异常详细信息，故使用此类封装，以保留异常信息。
     * */
    internal class MyHttpClient : IDisposable
    {
        private static void checkStringResponse(string responseString)
        {
            if (responseString.Equals("This gallery is currently unavailable.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(LocalizedStrings.Resources.GalleryRemoved);
            if (responseString.StartsWith("Your IP address has been temporarily banned for excessive pageloads"))
                throw new InvalidOperationException(LocalizedStrings.Resources.IPBannedOfPageLoad);
        }

        private void checkSadPanda(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentDisposition?.FileName == "sadpanda.jpg")
            {
                this.owner.ResetExCookie();
                throw new InvalidOperationException(LocalizedStrings.Resources.SadPanda);
            }
        }

        public MyHttpClient(Client owner, HttpClient inner)
        {
            this.inner = inner;
            this.owner = owner;
        }

        private void reformUri(ref Uri uri)
        {
            if (!uri.IsAbsoluteUri)
                uri = new Uri(this.owner.Uris.RootUri, uri);
        }

        private readonly HttpClient inner;
        private readonly HttpClient nocookie = new HttpClient();
        private readonly Client owner;

        public HttpRequestHeaderCollection DefaultRequestHeaders => this.inner.DefaultRequestHeaders;

        public IHttpAsyncOperation GetAsync(Uri uri, HttpCompletionOption completionOption, bool checkStatusCode)
        {
            reformUri(ref uri);
            var client = uri.Host.EndsWith("hentai.org") ? this.inner : this.nocookie;
            var request = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (completionOption == HttpCompletionOption.ResponseHeadersRead)
            {
                return request;
            }

            return Run<HttpResponseMessage, HttpProgress>(async (token, progress) =>
            {
                token.Register(request.Cancel);
                request.Progress = (t, p) => progress.Report(p);
                var response = await request;
                checkSadPanda(response);
                if (checkStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                }

                var buffer = response.Content.BufferAllAsync();
                if (!response.Content.TryComputeLength(out var length))
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    length = contentLength ?? ulong.MaxValue;
                }
                buffer.Progress = (t, p) =>
                {
                    progress.Report(new HttpProgress
                    {
                        TotalBytesToReceive = length,
                        BytesReceived = p,
                        Stage = HttpProgressStage.ReceivingContent
                    });
                };
                await buffer;
                return response;
            });
        }

        public IHttpAsyncOperation GetAsync(Uri uri)
        {
            return this.GetAsync(uri, HttpCompletionOption.ResponseContentRead, true);
        }

        public IAsyncOperationWithProgress<IBuffer, HttpProgress> GetBufferAsync(Uri uri)
        {
            reformUri(ref uri);
            return Run<IBuffer, HttpProgress>(async (token, progress) =>
            {
                var request = GetAsync(uri);
                token.Register(request.Cancel);
                request.Progress = (t, p) => progress.Report(p);
                var response = await request;
                return await response.Content.ReadAsBufferAsync();
            });
        }

        public IAsyncOperationWithProgress<IInputStream, HttpProgress> GetInputStreamAsync(Uri uri)
        {
            reformUri(ref uri);
            return Run<IInputStream, HttpProgress>(async (token, progress) =>
            {
                var request = GetAsync(uri);
                token.Register(request.Cancel);
                request.Progress = (t, p) => progress.Report(p);
                var response = await request;
                return await response.Content.ReadAsInputStreamAsync();
            });
        }

        public IAsyncOperationWithProgress<string, HttpProgress> GetStringAsync(Uri uri)
        {
            reformUri(ref uri);
            return Run<string, HttpProgress>(async (token, progress) =>
            {
                var request = GetAsync(uri);
                token.Register(request.Cancel);
                request.Progress = (t, p) => progress.Report(p);
                var response = await request;
                var str = await response.Content.ReadAsStringAsync();
                checkStringResponse(str);
                return str;
            });
        }

        public IAsyncOperationWithProgress<HtmlDocument, HttpProgress> GetDocumentAsync(Uri uri)
        {
            reformUri(ref uri);
            return Run<HtmlDocument, HttpProgress>(async (token, progress) =>
            {
                var request = this.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, false);
                token.Register(request.Cancel);
                request.Progress = (t, p) => progress.Report(p);
                var doc = new HtmlDocument();
                var response = await request;
                checkSadPanda(response);
                using (var stream = (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead())
                {
                    doc.Load(stream);
                }
                var rootNode = doc.DocumentNode;
                if (rootNode.ChildNodes.Count == 1 && rootNode.FirstChild.NodeType == HtmlNodeType.Text)
                    checkStringResponse(rootNode.FirstChild.InnerText);

                do
                {
                    if (response.StatusCode != HttpStatusCode.NotFound)
                        break;

                    var title = rootNode.Element("html").Element("head").Element("title");
                    if (title is null)
                        break;
                    if (!title.GetInnerText().StartsWith("Gallery Not Available - "))
                        break;

                    var error = rootNode.Element("html").Element("body")?.Element("div")?.Element("p");
                    if (error is null)
                        break;

                    var msg = error.GetInnerText();
                    switch (msg)
                    {
                    case "This gallery has been removed or is unavailable.":
                        throw new InvalidOperationException(LocalizedStrings.Resources.GalleryRemoved);
                    case "This gallery has been locked for review. Please check back later.":
                        throw new InvalidOperationException(LocalizedStrings.Resources.GalleryReviewing);
                    default:
                        throw new InvalidOperationException(msg);
                    }
                } while (false);
                response.EnsureSuccessStatusCode();
                HentaiVerseInfo.AnalyzePage(doc);
                return doc;
            });
        }

        public IHttpAsyncOperation PostAsync(Uri uri, IHttpContent content)
        {
            reformUri(ref uri);
            return this.inner.PostAsync(uri, content);
        }

        public IHttpAsyncOperation PostAsync(Uri uri, params KeyValuePair<string, string>[] content)
            => PostAsync(uri, (IEnumerable<KeyValuePair<string, string>>)content);

        public IHttpAsyncOperation PostAsync(Uri uri, IEnumerable<KeyValuePair<string, string>> content)
        {
            reformUri(ref uri);
            return this.inner.PostAsync(uri, new HttpFormUrlEncodedContent(content));
        }

        public IAsyncOperationWithProgress<string, HttpProgress> PostStringAsync(Uri uri, string content)
        {
            reformUri(ref uri);
            return Run<string, HttpProgress>(async (token, progress) =>
            {
                var op = PostAsync(uri, content is null ? null : new HttpStringContent(content));
                token.Register(op.Cancel);
                op.Progress = (sender, value) => progress.Report(value);
                var res = await op;
                res.EnsureSuccessStatusCode();
                var str = await res.Content.ReadAsStringAsync();
                checkStringResponse(str);
                return str;
            });
        }

        public IHttpAsyncOperation PutAsync(Uri uri, IHttpContent content)
        {
            reformUri(ref uri);
            return this.inner.PutAsync(uri, content);
        }

        public void Dispose()
        {
            this.inner.Dispose();
            this.nocookie.Dispose();
        }
    }
}
