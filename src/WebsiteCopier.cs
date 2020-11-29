using HtmlAgilityPack;
using log4net;
using System;
using System.IO;
using System.Linq;
using System.Net;

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
            if (TargetFolder.Exists) TargetFolder.Delete(true); // ensure the folder would be empty.
            if (!TargetFolder.Exists) TargetFolder.Create();
            GrabNode(StartPoint);
        }
        private void GrabNode(Uri uri, bool deleteExist = false)
        {
            if (uri.Authority != StartPoint.Authority) return;

            var req = WebRequest.CreateHttp(uri);
            try
            {
                using (var res = req.GetResponse())
                {
                    var file_path = uri.LocalPath;
                    if (file_path.EndsWith("/")) file_path += "index.html";
                    file_path = file_path.Replace(".aspx", ".html").Replace("/", "\\");
                    var file = new FileInfo(TargetFolder.FullName + file_path);
                    if (!file.Directory.Exists) file.Directory.Create();
                    if (deleteExist && file.Exists) file.Delete();
                    file.Refresh();
                    if (file.Exists) return;

                    if (res.ContentType.Contains("text"))
                    {
                        string content = null;
                        using (var sr = new StreamReader(res.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            using (var sw = file.CreateText())
                                sw.Write(content.Replace(".aspx", ".html"));
                        }
                        if (res.ContentType.Contains("text/html"))
                        {
                            var s = uri.AbsolutePath.Split('/');
                            var tmp = uri.Scheme + "://" + uri.Authority + string.Join("/", s.Take(s.Length - 1).ToArray());
                            uri = new Uri(tmp);
                            var doc = new HtmlDocument();
                            doc.LoadHtml(content);
                            foreach (var node in doc.DocumentNode.ChildNodes)
                                Handle(node, uri);
                        }
                    }
                    else
                    {
                        using (var fs = file.Create())
                            res.GetResponseStream().CopyTo(fs);
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Message.Contains("404"))
                    _logger.Warn(ex.Message + "[" + uri + "]", ex);
                else
                {
                    _logger.Error(ex.Message + "[" + uri + "]", ex);
                    throw;
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message + "[" + uri + "]", ex);
                throw;
            }
        }
        private static string Combine(Uri uri, string path)
        {
            if (path.StartsWith("/"))
                return uri.Scheme + "://" + uri.Authority + path;

            return uri.AbsoluteUri + "/" + path;
        }
        private void Handle(HtmlNode node, Uri currentUri)
        {
            _logger.DebugFormat("node(type={0} / name={1} / currentUri={2})", node.GetType(), node.Name, currentUri);
            try
            {
                switch (node.Name.ToLower())
                {
                    case "a":
                    case "link":
                        {
                            var url = node.Attributes["href"]?.Value;
                            if (!string.IsNullOrWhiteSpace(url) && !url.Contains("javascript"))
                            {
                                if (!url.StartsWith("http")) url = Combine(currentUri, url);
                                GrabNode(new Uri(url));
                            }
                        }
                        break;
                    case "img":
                    case "script":
                        {
                            var url = node.Attributes["src"]?.Value;
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                if (!url.StartsWith("http")) url = Combine(currentUri, url);
                                GrabNode(new Uri(url));
                            }
                        }
                        break;
                    default:
                        _logger.Warn(node.Name + " no need to process");
                        break;
                }
            }
            catch (UriFormatException e)
            {
                _logger.Error(node.OuterHtml + " " + e.Message, e);
            }
            foreach (var subNode in node.ChildNodes)
                Handle(subNode, currentUri);
        }
    }
}
