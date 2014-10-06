using Common.Logging;
using Hangfire;
using Mizore.CommunicationHandler.RequestHandler;
using Mizore.CommunicationHandler.ResponseHandler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Net.Sockets;
using System.Threading.Tasks;
using File = LANSearch.Data.Search.Solr.File;

namespace LANSearch.Data.Jobs.Ftp
{
    public class FtpCrawler
    {
        private readonly static DateTime _minDate = new DateTime(1, 1, 2);
        protected ILog Logger = LogManager.GetCurrentClassLogger();

        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public FtpStatus CheckServer(Server.Server server)
        {
            var ftp = new FtpClient { Host = server.Address, Port = server.Port };
            if (!string.IsNullOrWhiteSpace(server.Login))
                ftp.Credentials = new NetworkCredential(server.Login, server.Password);

            FtpListItem[] unused;
            return CheckServer(ftp, out unused);
        }

        private FtpStatus CheckServer(FtpClient client, out FtpListItem[] list)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            var status = new FtpStatus();
            try
            {
                list = client.GetListing("/");
            }
            catch (FtpCommandException fce)
            {
                list = null;
                int code;
                int.TryParse(fce.CompletionCode, out code);
                status.ErrorFtpCode = code;
                status.ErrorFtpMessage = fce.Message;
                if (code == 530 || code == 430 || code == 332)
                    status.ErrorType = FtpStatus.FtpErrorType.Login;
                else if (code == 421)
                    status.ErrorType = FtpStatus.FtpErrorType.Rejected;
                else
                    status.ErrorType = FtpStatus.FtpErrorType.Unknown;
                return status;
            }
            catch (SocketException se)
            {
                list = null;
                status.ErrorType = FtpStatus.FtpErrorType.Offline;
                status.ErrorFtpMessage = se.Message;
                return status;
            }
            status.ErrorType = FtpStatus.FtpErrorType.None;
            return status;
        }

        public void CrawlServers()
        {
            if (!Ctx.SearchManager.SolrServer.IsOnline)
            {
                Logger.Info("CrawlServers stopped because solr is offline.");
                return;
            }
            var servers = Ctx.ServerManager.GetAll().Where(x => !x.NoScans).ToList();
            if (!servers.Any())
            {
                Logger.Info("CrawlServers stopped because no server is set.");
                return;
            }
            Task.WaitAll(servers.Select(CrawlServer).ToArray());
            Ctx.JobManager.EnqueueJob(() => Ctx.JobManager.NotificationJob.NotifyAll());
        }

        public void CrawlServer(int id, bool force)
        {
            if (!Ctx.SearchManager.SolrServer.IsOnline)
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because solr is offline.", id);
                return;
            }
            var server = Ctx.ServerManager.Get(id);
            if (server == null)
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because id is invalid (server object is null).", id);
                return;
            }
            if (!force && (server.NoScans || server.Deleted))
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because NoScans or Deleted is set.", id);
                return;
            }
            if (Ctx.RedisManager.FtpCrawlerServerIsLocked(id))
            {
                Logger.WarnFormat("Prevented crawling for server {0} because Redis lock is still engaged", id);
                return;
            }
            var task = CrawlServer(server);
            task.Wait();
            Ctx.RedisManager.FtpCrawlerServerUnlock(id);
            Ctx.JobManager.EnqueueJob(() => Ctx.JobManager.NotificationJob.NotifyServer(id));
        }

        private Task CrawlServer(Server.Server server)
        {
            return Task.Factory.StartNew(() =>
            {
                Logger.InfoFormat("Started FTP Crawling for server {0}.", server.Id);
                var queue = new BlockingCollection<FtpListItem>(1000);
                Task.Run(() => CrawlFtpServer(server, queue));
                int count = 0;
                var files = new List<File>();
                foreach (var item in queue.GetConsumingEnumerable())
                {
                    count++;
                    var file = new File
                    {
                        ServerId = server.Id,
                        Path = item.FullName,
                        Size = item.Size,
                        DateLastSeen = DateTime.Now
                    };
                    file.Id = string.Format("{0}::{1}", file.ServerId, file.Path);
                    file.Extension = Path.GetExtension(file.Path);
                    files.Add(file);
                    if (files.Count >= 500)
                    {
                        DoSolrIndex(files);
                        files.Clear();
                    }
                }
                if (files.Count > 0)
                {
                    DoSolrIndex(files);
                }
                Logger.InfoFormat("DONE: Server {0} is crawled, indexed {1} files.", server.Id, count);
            });
        }

        private void DoSolrIndex(IEnumerable<File> list)
        {
            try
            {
                var updateRequest = new UpdateRequest(Ctx.SearchManager.SolrServer.GetUriBuilder());
                foreach (var file in list)
                {
                    var getRequest = new GetRequest(Ctx.SearchManager.SolrServer.GetUriBuilder(), file.Id);
                    GetResponse response;
                    if (Ctx.SearchManager.SolrServer.TryRequest(getRequest, out response) && response.Document != null)
                    {
                        var indexedFile = response.GetObject<File>(Ctx.SearchManager.SolrMapper);
                        if (indexedFile != null)
                            file.DateFirstSeen = indexedFile.DateFirstSeen;
                    }
                    if (file.DateFirstSeen <= _minDate)
                    {
                        file.DateFirstSeen = DateTime.Now;
                    }

                    var doc = Ctx.SearchManager.SolrMapper.GetDocument(file);
                    updateRequest.Add(doc);
                }
                //This has to throw an error if there is a problem, do not use TryRequest here.
                Ctx.SearchManager.SolrServer.Request<UpdateResponse>(updateRequest);
            }
            catch (Exception e)
            {
                Logger.Error("Exception occurred while updating solr", e);
            }
        }

        private void CrawlFtpServer(Server.Server server, BlockingCollection<FtpListItem> queue)
        {
            var ftp = new FtpClient { Host = server.Address, Port = server.Port };
            if (!string.IsNullOrWhiteSpace(server.Login))
                ftp.Credentials = new NetworkCredential(server.Login, server.Password);
            else
                ftp.Credentials = new NetworkCredential("anonymous", "LANSearch");
            FtpListItem[] root;
            var status = CheckServer(ftp, out root);
            if (!status.IsOk)
            {
                queue.CompleteAdding();
                if (ftp.IsConnected)
                    ftp.Disconnect();
                server.ScanFailedLastDate = DateTime.Now;
                server.ScanFailedAttempts++;
                if (status.ErrorType == FtpStatus.FtpErrorType.Offline)
                {
                    server.Online = false;
                    server.ScanFailedMessage=string.Format("Connection couldn't be established, server is offline.");
                    Ctx.ServerManager.Save(server);
                    Logger.WarnFormat("Server {0} is Offline.",server.Id);
                    return;
                }
                server.ScanFailedMessage = string.Format("{0} {1}", status.ErrorFtpCode, status.ErrorFtpMessage);
                if (server.Online && server.ScanFailedAttempts > Ctx.Config.CrawlerOfflineLimit)
                    server.Online = false;
                Logger.WarnFormat("Login Error for Server {0}: {1}",server.Id,server.ScanFailedMessage);
                Ctx.ServerManager.Save(server);
                return;
            }
            if (!server.Online || server.ScanFailedAttempts > 0)
            {
                server.Online = true;
                server.ScanFailedAttempts = 0;
                server.ScanFailedMessage = null;
                Logger.InfoFormat("Reseted online status and failed attempts for server {0}",server.Id);
            }
            var visited = new HashSet<string>();
            try
            {
                GetList(ftp, "/", queue, visited, root);
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Exception while crawling server {0}", e, server.Id);
            }
            finally
            {
                queue.CompleteAdding();
                if (ftp.IsConnected)
                    ftp.Disconnect();
            }
        }

        protected HashSet<string> IgnoredFiles = new HashSet<string> { "Thumbs.db", "desktop.ini", ".DS_Store", ".apdisk", ".com.apple.timemachine.supported", ".Parent", ".iTunes Preferences.plist", ".directory" };
        protected HashSet<string> IgnoredFolders = new HashSet<string> { ".@__thumb", ".AppleDouble", ".localized" };

        private void GetList(FtpClient client, string path, BlockingCollection<FtpListItem> collection, HashSet<string> visited, FtpListItem[] preRequested = null)
        {
            if (visited.Contains(path)) return;
            visited.Add(path);
            var items = preRequested ?? client.GetListing(path, FtpListOption.AllFiles | FtpListOption.Size);
            var toCheck = new List<string>();
            foreach (var item in items)
            {
                switch (item.Type)
                {
                    case FtpFileSystemObjectType.File:
                        if (!IgnoredFiles.Contains(item.Name))
                            collection.Add(item);
                        break;

                    case FtpFileSystemObjectType.Directory:
                        if (!IgnoredFolders.Contains(item.Name) && !visited.Contains(item.FullName))
                            toCheck.Add(item.FullName);
                        break;

                    case FtpFileSystemObjectType.Link:
                        //Note: resolving symlinks shouldn't be necessary , however it makes crawling a LOT slower.
                        //due to that fact, i currently removed the fixed crawling code.

                        //var resolvedLink = ResolveLinkPath(path, item.LinkTarget);
                        //item.LinkTarget = resolvedLink;
                        //var linkTarget = client.DereferenceLink(item);
                        //if (linkTarget != null)
                        //{
                        //    if (linkTarget.Type == FtpFileSystemObjectType.File)
                        //    {
                        //        if (!IgnoredFiles.Contains(item.Name))
                        //            collection.Add(item);
                        //        continue;
                        //    }
                        //    if (!IgnoredFolders.Contains(item.Name) && !visited.Contains(item.FullName))
                        //        toCheck.Add(linkTarget.FullName);
                        //}
                        break;
                }
            }
            if (toCheck.Count > 0)
            {
                foreach (var folder in toCheck)
                {
                    GetList(client, folder, collection, visited);
                }
            }
        }

        //private static string ResolveLinkPath(string current, string relative)
        //{
        //    if (string.IsNullOrWhiteSpace(relative))
        //        return current;
        //    var relativeSplit = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        //    var pathSegments = new List<string>();
        //    if (relative.StartsWith("/"))
        //    {
        //        foreach (var folder in relativeSplit)
        //        {
        //            switch (folder)
        //            {
        //                case "":
        //                case ".":
        //                    continue;
        //                case "..":
        //                    if (pathSegments.Count>0)
        //                        pathSegments.RemoveAt(pathSegments.Count - 1);
        //                    break;
        //                default:
        //                    pathSegments.Add(folder);
        //                    break;
        //            }
                    
        //        }
        //        return "/"+string.Join("/", pathSegments);
        //    }
        //    var currentSplit = current.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        //    int removeCount = 0;
        //    foreach (var folder in relativeSplit)
        //    {
        //        switch (folder)
        //        {
        //            case "":
        //            case ".":
        //                continue;
        //            case "..":
        //                if (pathSegments.Count == 0)
        //                    removeCount++;
        //                else
        //                    pathSegments.RemoveAt(pathSegments.Count - 1);
        //                break;
        //            default:
        //                if (pathSegments.Count == 0)
        //                {
        //                    foreach (var currentFolder in currentSplit.Reverse())
        //                    {
        //                        if (removeCount > 0)
        //                        {
        //                            removeCount--;
        //                        }
        //                        else
        //                        {
        //                            pathSegments.Insert(0, currentFolder);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    pathSegments.Add(folder);
        //                }
        //                break;
        //        }
        //    }
        //    return string.Join("/", pathSegments);
        //}
    }
}