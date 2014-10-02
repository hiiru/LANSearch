using Common.Logging;
using LANSearch.Data.Mail;
using Microsoft.Owin.Hosting;

namespace LANSearch
{
    using System;

    internal class Program
    {
        private static int Main(string[] args)
        {
            var logger=LogManager.GetCurrentClassLogger();
            logger.InfoFormat("LANSearch started{0}.", (args.Length == 0 ? "" : ", args: " + string.Join(" ", args)));
            var status = InitConfig.Init(args);
            if (status != 0)
            {
                logger.Info("LANSearch ended due to invalid arguments.");
                return status;
            }

            try
            {
                var url = string.Format("http://{0}:{1}", InitConfig.ListenHost, InitConfig.ListenPort);
                using (WebApp.Start<Startup>(url))
                {
                    Console.WriteLine("Running on {0}", url);
                    Console.WriteLine("Press enter to exit");
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                logger.Fatal("Unhandled Exception, server is halted.", e);
                return 2;
            }
            logger.Info("LANSearch Stopped.");
            return 0;
        }
    }
}