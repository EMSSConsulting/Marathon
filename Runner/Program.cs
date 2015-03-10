using System;
using System.Linq;
using static System.Console;

namespace Marathon
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) { ShowHelp(); return; }

            Title = "GitLab CI Runner";

            var runner = new Runner(args.Skip(1).ToArray());
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
                    runner.Setup().Wait();
                    return;

                case "start":
                    runner.Run().Wait();
                    return;
            }
        }

        static void ShowHelp()
        {
            WriteLine("Gitlab CI Runner for Windows");
            WriteLine("Marathon COMMAND [OPTIONS]");
            WriteLine();
            WriteLine("Commands:");
            WriteLine(" help     Shows this help page.");
            WriteLine(" setup    Runs a guided setup tool to assist you in configuring the runner.");
            WriteLine(" start    Starts the runner as a command line service.");
            WriteLine();
            WriteLine("Options:");
            WriteLine(" -url=    Sets the coordinator URL (https://ci.gitlab.org)");
            WriteLine(" -token=  Sets the token used to authenticate with the coordinator");
            WriteLine(" -shell=  Sets the command line shell to be used by your scripts (cmd/powershell)");
            WriteLine(" -build_path=  Sets the path in which builds will be conducted");
        }
    }
}
