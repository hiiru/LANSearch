using Microsoft.Owin.Hosting;

namespace LANSearch
{
    using System;

    internal class Program
    {
        private static void Main(string[] args)
        {
            InitConfig.Init(args);

            var url = string.Format("http://{0}:{1}", InitConfig.ListenHost, InitConfig.ListenPort);

            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Running on {0}", url);
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }
    }
}