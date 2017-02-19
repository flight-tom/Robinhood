using HtmlAgilityPack;
using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Doway.Tools.Robinhood
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            GlobalContext.Properties["appname"] = "Robinhood";
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            try
            {
                string url = args[0];
                using (var client = new WebClient())
                {
                    string responseHtml = Encoding.UTF8.GetString(client.DownloadData(url));
                    Uri uri = new Uri(url);
                    HtmlDocument doc = new HtmlDocument();
                    bool uriChanged = false;
                    SaveFile(uri, responseHtml, false, out uriChanged);
                    doc.LoadHtml(responseHtml);
                    foreach (var node in doc.DocumentNode.ChildNodes)
                        Handle(node, client, ref uri);

                    responseHtml = doc.DocumentNode.InnerHtml;
                    SaveFile(uri, responseHtml, true, out uriChanged);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message, ex);
            }
            finally
            {
                Console.ReadLine();
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
                            bool uriChanged = false;
                            string url = (null == node.Attributes["href"]) ? null : node.Attributes["href"].Value;
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                Uri uri = new Uri(url);
                                if (uri.Authority == orgUri.Authority)
                                {
                                    byte[] data = client.DownloadData(url);
                                    string localPath = uri.LocalPath.ToLower();
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
                                        string responseHtml = Encoding.UTF8.GetString(data);
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
                            bool uriChanged = false;
                            string url = (null == node.Attributes["src"]) ? null : node.Attributes["src"].Value;
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                Uri uri = new Uri(url);
                                if (uri.Authority == orgUri.Authority)
                                {
                                    byte[] data = client.DownloadData(url);
                                    string localPath = uri.LocalPath.ToLower();
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
                                        string responseHtml = Encoding.UTF8.GetString(data);
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
            string file_name = uri.LocalPath;
            uriChanged = false;
            if (file_name.EndsWith("/"))
            {
                file_name += "index.html";
                uri = new Uri(uri.OriginalString + "index.html");
                uriChanged = true;
            }
            file_name = file_name.Replace('/','\\');
            if (file_name.StartsWith("\\"))
                file_name = file_name.Remove(0, 1);

            FileInfo f = new FileInfo(file_name);
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
            string file_name = uri.LocalPath;
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

            FileInfo f = new FileInfo(file_name);
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
