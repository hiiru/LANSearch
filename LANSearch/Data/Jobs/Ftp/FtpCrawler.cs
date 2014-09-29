using Common.Logging;
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
            if (!Ctx.SolrServer.IsOnline)
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
        }

        public void CrawlServer(int id)
        {
            if (!Ctx.SolrServer.IsOnline)
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because solr is offline.",id);
                return;
            }
            var server = Ctx.ServerManager.Get(id);
            if (server == null)
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because id is invalid (server object is null).", id);
                return;
            }
            if (server.NoScans)
            {
                Logger.InfoFormat("CrawlServer for id {0} stopped because NoScans is set.", id);
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
                var updateRequest = new UpdateRequest(Ctx.SolrServer.GetUriBuilder());
                foreach (var file in list)
                {
                    var doc = Ctx.SolrMapper.GetDocument(file);
                    updateRequest.Add(doc);
                }
                //This has to throw an error if there is a problem, do not use TryRequest here.
                Ctx.SolrServer.Request<UpdateResponse>(updateRequest);
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
                    Ctx.ServerManager.Save(server);
                    return;
                }
                server.ScanFailedMessage = string.Format("{0} {1}", status.ErrorFtpCode, status.ErrorFtpMessage);
                if (server.Online && server.ScanFailedAttempts > Ctx.Config.CrawlerOfflineLimit)
                    server.Online = false;
                Ctx.ServerManager.Save(server);
                return;
            }

            var visited = new Dictionary<string, bool>();
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

        private void GetList(FtpClient client, string path, BlockingCollection<FtpListItem> collection, Dictionary<string, bool> visited, FtpListItem[] preRequested = null)
        {
            var items = preRequested ?? client.GetListing(path, FtpListOption.AllFiles | FtpListOption.Size);
            visited[path] = true;
            var toCheck = new List<string>();
            foreach (var item in items)
            {
                switch (item.Type)
                {
                    case FtpFileSystemObjectType.File:
                        collection.Add(item);
                        break;

                    case FtpFileSystemObjectType.Directory:
                        if (!visited.ContainsKey(item.FullName))
                            toCheck.Add(item.FullName);
                        break;

                    case FtpFileSystemObjectType.Link:
                        if (!visited.ContainsKey(item.LinkTarget))
                            toCheck.Add(item.LinkTarget);
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
    }
}