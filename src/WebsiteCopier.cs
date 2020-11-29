using HtmlAgilityPack;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Doway.Tools.Robinhood
{
    public class WebsiteCopier
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(WebsiteCopier));
        public WebsiteCopier(string url, string savePath) : this(new Uri(url), new DirectoryInfo(savePath)) { }
        public WebsiteCopier(Uri uri, DirectoryInfo saveTarget)
        {
            StartPoint = uri;
            TargetFolder = saveTarget;
        }
        public Uri StartPoint { get; private set; }
        public DirectoryInfo TargetFolder { get; private set; }
        public void StartCopy()
        {
            if (TargetFolder.Exists) TargetFolder.Delete(); // ensure the folderwould be empty.
            if (!TargetFolder.Exists) TargetFolder.Create();
            GrabNode(StartPoint);
        }
        private void GrabNode(Uri uri)
        {
            var req = WebRequest.CreateHttp(uri);
            using (var res = req.GetResponse())
            {
                if (res.ContentType.Contains("text"))
                {
                    using (var sr = new StreamReader(res.GetResponseStream()))
                    {
                    }
                }
            }
        }
        private static void Handle(HtmlNode node, WebClient client, ref Uri orgUri)
        {
            _logger.DebugFormat("node(type={0} / name={1})", node.GetType(), node.Name);
            try
            {
                switch (node.Name.ToLower())
                {
                    case "a":
                    case "link":
                        {
                            var uriChanged = false;
                            var url = node.Attributes["href"]?.Value;
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                var uri = new Uri(url);
                                if (uri.Authority == orgUri.Authority)
                                {
                                    var data = client.DownloadData(url);
                                    var localPath = uri.LocalPath.ToLower();
                                    if (localPath.EndsWith(".jpg") ||
                                        localPath.EndsWith(".jpeg") ||
                                        localPath.EndsWith(".gif") ||
                                        localPath.EndsWith(".png") ||
                                        localPath.EndsWith(".ico") ||
                                        localPath.EndsWith(".zip"))
                                    {
                                        var u = SaveFile(uri, data, false, out uriChanged);
                                        url = u.OriginalString;
                                    }
                                    else
                                    {
                                        var responseHtml = Encoding.UTF8.GetString(data);
                                        var u = SaveFile(uri, responseHtml, false, out uriChanged);
                                        url = u.OriginalString;
                                    }
                                    node.Attributes["href"].Value = url.Substring(url.IndexOf(orgUri.Authority) + orgUri.Authority.Length + 1);
                                }
                            }
                        }
                        break;
                    case "img":
                    case "script":
                        {
                            var uriChanged = false;
                            var url = node.Attributes["src"]?.Value;
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                var uri = new Uri(url);
                                if (uri.Authority == orgUri.Authority)
                                {
                                    var data = client.DownloadData(url);
                                    var localPath = uri.LocalPath.ToLower();
                                    if (localPath.EndsWith(".jpg") ||
                                        localPath.EndsWith(".jpeg") ||
                                        localPath.EndsWith(".gif") ||
                                        localPath.EndsWith(".png") ||
                                        localPath.EndsWith(".ico") ||
                                        localPath.EndsWith(".zip"))
                                    {
                                        var u = SaveFile(uri, data, false, out uriChanged);
                                        url = u.OriginalString;
                                    }
                                    else
                                    {
                                        var responseHtml = Encoding.UTF8.GetString(data);
                                        var u = SaveFile(uri, responseHtml, false, out uriChanged);
                                        url = u.OriginalString;
                                    }
                                    node.Attributes["src"].Value = url.Substring(url.IndexOf(orgUri.Authority) + orgUri.Authority.Length + 1);
                                }
                            }
                        }
                        break;
                }
            }
            catch (UriFormatException) { }
            if (node.ChildNodes.Count > 0)
                foreach (var subNode in node.ChildNodes)
                    Handle(subNode, client, ref orgUri);
        }

        private static Uri SaveFile(Uri uri, string content, bool overwrite, out bool uriChanged)
        {
            var file_name = uri.LocalPath;
            uriChanged = false;
            if (file_name.EndsWith("/"))
            {
                file_name += "index.html";
                uri = new Uri(uri.OriginalString + "index.html");
                uriChanged = true;
            }
            file_name = file_name.Replace('/', '\\');
            if (file_name.StartsWith("\\"))
                file_name = file_name.Remove(0, 1);

            var f = new FileInfo(file_name);
            if (!f.Exists || overwrite)
            {
                if (!f.Directory.Exists)
                    f.Directory.Create();

                using (var sw = new StreamWriter(f.FullName, false))
                    sw.Write(content);
            }
            return uri;
        }

        private static Uri SaveFile(Uri uri, byte[] content, bool overwrite, out bool uriChanged)
        {
            var file_name = uri.LocalPath;
            uriChanged = false;
            if (file_name.EndsWith("/"))
            {
                file_name += "index.html";
                uri = new Uri(uri.OriginalString + "index.html");
                uriChanged = true;
            }
            file_name = file_name.Replace('/', '\\');
            if (file_name.StartsWith("\\"))
                file_name = file_name.Remove(0, 1);

            var f = new FileInfo(file_name);
            if (!f.Exists || overwrite)
            {
                if (!f.Directory.Exists)
                    f.Directory.Create();

                f.Delete();

                using (var sw = f.Create())
                    sw.Write(content, 0, content.Length);
            }
            return uri;
        }
    }
}
