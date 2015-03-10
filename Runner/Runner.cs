using Microsoft.Framework.ConfigurationModel;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon
{
    public class Runner
    {
        public Runner(string[] args)
        {
            Configuration = new Configuration()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Add(new Helpers.WritableJsonConfigurationSource("config.json"));

            Shells.ShellBase.Register(new Shells.PowerShell(), new Shells.CommandPrompt());

            Initialize();
        }


        private static Logger Log = LogManager.GetCurrentClassLogger();

        public IConfiguration Configuration { get; private set; }

        public RestClient Client { get; private set; }

        public bool Running { get; protected set; }

        #region Helper Classes

        public Network Network { get; private set; }

        public Shells.ShellBase Shell { get; private set; }

        #endregion

        public async Task Run()
        {
            Log.Info("GitLab CI Runner started");
            while (Running)
            {
                await Task.Delay(5000);

                var buildInfo = await Network.RequestBuild();
                if (buildInfo == null) continue;

                Log.Info("Starting build #{0} for {1}", buildInfo.ID, buildInfo.ProjectName);
                Log.Debug("{0}...{1}", buildInfo.BeforeSHA, buildInfo.SHA);

                var build = new Build(this, buildInfo);

                var result = await build.PerformBuild();

                if (result.Success)
                    Log.Info("Build #{0} completed", buildInfo.ID);
                else
                    Log.Warn("Build #{0} failed", buildInfo.ID);

#pragma warning disable CS4014
                Task.Run(async () =>
#pragma warning restore CS4014
                {
                    var updateResponse = NetworkResponse.Failure;
                    while (updateResponse == NetworkResponse.Failure)
                    {
                        Log.Trace("Updating server build status for build #{0}", buildInfo.ID);
                        if (result.Success) updateResponse = await Network.UpdateBuild(buildInfo, BuildState.success, result.Output);
                        else updateResponse = await Network.UpdateBuild(buildInfo, BuildState.failed, result.Output);

                        if(updateResponse == NetworkResponse.Failure)
                            await Task.Delay(5000);
                    }

                    Log.Trace("Server updated for build #{0}", buildInfo.ID);
                });
            }
        }

        public async Task Setup()
        {
            var url = Configuration.Get<string>("url");
            while (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("Please enter the gitlab-ci coordinator URL (e.g. http://gitlab-ci.org/ )");
                Console.Write("url: ");
                url = Console.ReadLine();
            }

            Configuration.Set("url", url);

            Initialize();

            while (true)
            {
                var token = Configuration.Get<string>("REGISTRATION_TOKEN");
                var description = Configuration.Get<string>("RUNNER_DESCRIPTION") ?? Environment.MachineName;
                var tagList = Configuration.Get<string>("RUNNER_TAG_LIST") ?? "win32";

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Please enter the GitLab CI registration token for this runner");
                    Console.Write("token: ");
                    token = Console.ReadLine();
                }

                Console.WriteLine("Registering runner as {0} with registration token {1} on {2}.", description, token, url);
                var runner = await Network.RegisterRunner(token, description, tagList.Split(','));
                if (runner != null)
                {
                    Configuration.Set("token", runner.Token);
                    Console.WriteLine("Runner registered successfully. Run 'Marathon start' to start it.");
                    return;
                }
                else
                {
                    Console.WriteLine("Failed to register this runner, please make sure your token was correct.");
                }
            }
        }

        protected void Initialize()
        {
            Log.Debug("Using shell environment: {0}", Configuration.Get("shell") ?? "cmd");
            Shell = Shells.ShellBase.GetShell(Configuration.Get("shell") ?? "cmd");

            Network = new Network(this);

            if (!string.IsNullOrEmpty(Configuration.Get<string>("url")))
            {
                Log.Debug("Using coordinator: {0}", Configuration.Get<string>("url"));
                Client = new RestClient(Configuration.Get<string>("url"));

                Running = true;
            }
        }
    }
}
