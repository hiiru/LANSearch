using System;
using System.Collections.Generic;

namespace LANSearch
{
    public static class InitConfig
    {
        public static void Init(string[] args)
        {
            InitDefault();
            if (args.Length <= 0) return;
            bool invalid = false;
            foreach (var arg in args)
            {
                var splited = arg.Split('=');
                if (splited.Length != 2)
                {
                    invalid = true;
                    break;
                }
                int value;
                switch (splited[0].TrimStart('-').ToLower())
                {
                    case "host":
                        ListenHost = splited[1];
                        break;

                    case "port":
                        if (!int.TryParse(splited[1], out value))
                        {
                            invalid = true;
                            break;
                        }
                        ListenPort = value;
                        break;

                    case "setup.ips":
                        var ipSplited = splited[1].Split(',');
                        foreach (var ip in ipSplited)
                        {
                            SetupIps.Add(ip);
                        }
                        break;

                    case "redis.host":
                        RedisServer = splited[1];
                        break;

                    case "redis.port":
                        if (!int.TryParse(splited[1], out value))
                        {
                            invalid = true;
                            break;
                        }
                        RedisPort = value;
                        break;

                    case "redis.password":
                        RedisPassword = splited[1];
                        break;

                    case "redis.db.app":
                        if (!int.TryParse(splited[1], out value))
                        {
                            invalid = true;
                            break;
                        }
                        RedisDbApp = value;
                        break;

                    case "redis.db.jobs":
                        if (!int.TryParse(splited[1], out value))
                        {
                            invalid = true;
                            break;
                        }
                        RedisDbHangfire = value;
                        break;

                    default:
                        invalid = true;
                        break;
                }
                if (invalid)
                    break;
            }
            if (invalid)
            {
                PrintHelp();
                Environment.Exit(1);
            }
        }

        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Invalid argument!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("-host={ip}\t\tWebserver Listen IP.");
            Console.WriteLine("-port={port}\t\tWebserver Listen Port.");
            Console.WriteLine("-setup.ips={ips}\tAllowed Setup Ips (csv).");
            Console.WriteLine("-redis.host={ip}\tRedis server IP.");
            Console.WriteLine("-redis.port={port}\tRedis server Port.");
            Console.WriteLine("-redis.password={ip}\tRedis server password.");
            Console.WriteLine("-redis.db.app={ip}\tRedis DB for the App.");
            Console.WriteLine("-redis.db.jobs={ip}\tRedis DB for Jobs.");
        }

        private static void InitDefault()
        {
            ListenHost = "+";
            ListenPort = 8080;

            SetupIps=new List<string>
            {
                "127.0.0.1",
                "::1"
            };

            RedisServer = "127.0.0.1";
            RedisPort = 6379;
            RedisDbApp = 0;
            RedisDbHangfire = 1;
            RedisPassword = null;

        }

        public static string ListenHost { get; private set; }

        public static int ListenPort { get; private set; }

        public static List<string> SetupIps { get; private set; }

        public static string RedisServer { get; private set; }

        public static int RedisPort { get; private set; }

        public static int RedisDbApp { get; private set; }

        public static int RedisDbHangfire { get; private set; }

        public static string RedisPassword { get; private set; }
    }
}