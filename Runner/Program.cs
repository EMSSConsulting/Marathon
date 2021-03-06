﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace Marathon
{
    class Program
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            if (args.Length == 0) { ShowHelp(); return; }

            Console.Title = "GitLab CI Runner";

            var runner = new Runner(args.Skip(1).ToArray());
            Task waitFor = null;
            switch (args[0])
            {
                case "help":
                case "/?":
                case "-?":
                case "--?":
                case "/help":
                case "-help":
                case "--help":
                    ShowHelp();
                    return;

                case "setup":
                    waitFor = runner.Setup();
                    break;

                case "clean":
                    waitFor = runner.Clean();
                    break;

                case "start":
                    waitFor = runner.Run();
                    break;
            }

            Console.CancelKeyPress += (o, e) =>
            {
                Log.Info("Close signal received - shutting down when the current build compltes.");
                runner.Running = false;
            };

            if (waitFor != null) waitFor.Wait();
        }

        static void ShowHelp()
        {
            Console.WriteLine("Gitlab CI Runner for Windows");
            Console.WriteLine("Marathon COMMAND [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine(" help     Shows this help page.");
            Console.WriteLine(" setup    Runs a guided setup tool to assist you in configuring the runner.");
            Console.WriteLine(" start    Starts the runner as a command line service.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine(" -url=    Sets the coordinator URL (https://ci.gitlab.org)");
            Console.WriteLine(" -token=  Sets the token used to authenticate with the coordinator");
            Console.WriteLine(" -shell=  Sets the command line shell to be used by your scripts (cmd/powershell)");
            Console.WriteLine(" -build_path=  Sets the path in which builds will be conducted");
        }
    }
}
