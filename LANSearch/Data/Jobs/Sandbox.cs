using System;
using System.Threading;

namespace LANSearch.Data.Jobs
{
    public static class Sandbox
    {
        public static void FirstJob()
        {
            Thread.Sleep(2000);
            Console.WriteLine("I was called :)");
        }
    }
}