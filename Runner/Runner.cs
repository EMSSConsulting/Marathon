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

        public IConfiguration Configuration { get; protected set; }

        public RestClient Client { get; protected set; }

        public bool Running { get; protected set; }

        #region Helper Classes

        public Network Network { get; protected set; }

        public Shells.ShellBase Shell { get; protected set; }

        #endregion

        public async Task Run()
        {
            Log.Debug("Checking configuration");
            if(!CheckConfiguration())
            {
                Log.Warn("GitLab CI Runner has not been configured correctly");
                return;
            }

            Log.Info("GitLab CI Runner started");
            while (Running)
            {

                var build = await FetchBuild();
                if (build == null)
                {
                    await Task.Delay(5000);
                    continue;
                }

                await OnBuildStarting(build);

                var result = await build.PerformBuild();

#pragma warning disable 4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
#pragma warning restore 4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                {
                    await OnBuildCompleted(build, result);
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

        protected virtual void Initialize()
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

        protected virtual bool CheckConfiguration()
        {
            var requiredProperties = new[] { "url", "token" };

            bool validConfig = true;
            foreach(var requiredProperty in requiredProperties)
            {
                Log.Debug("Checking configuration for {0}.", requiredProperty);
                if(string.IsNullOrEmpty(Configuration.Get(requiredProperty)))
                {
                    validConfig = false;
                    Log.Warn("Configuration was missing a value for {0}.", requiredProperty);
                }
            }

            return validConfig;
        }

        protected virtual async Task<Build> FetchBuild()
        {
            var buildInfo = await Network.RequestBuild();
            if (buildInfo == null) return null;

            return new Build(this, buildInfo);
        }

        protected virtual async Task OnBuildStarting(Build build)
        {
            Log.Info("Starting build #{0} for {1}", build.BuildInfo.ID, build.BuildInfo.ProjectName);
            Log.Debug("{0}...{1}", build.BuildInfo.BeforeSHA, build.BuildInfo.SHA);

            await Task.Yield();
        }

        protected virtual async Task OnBuildCompleted(Build build, Models.BuildResult result)
        {
            if (result.Success)
                Log.Info("Build #{0} completed", build.BuildInfo.ID);
            else
                Log.Warn("Build #{0} failed", build.BuildInfo.ID);

            var updateResponse = NetworkResponse.Failure;
            while (updateResponse == NetworkResponse.Failure)
            {
                Log.Trace("Updating server build status for build #{0}", build.BuildInfo.ID);
                if (result.Success) updateResponse = await Network.UpdateBuild(build.BuildInfo, BuildState.success, result.Output);
                else updateResponse = await Network.UpdateBuild(build.BuildInfo, BuildState.failed, result.Output);

                if (updateResponse == NetworkResponse.Failure)
                    await Task.Delay(5000);
            }

            Log.Trace("Server updated for build #{0}", build.BuildInfo.ID);
        }
    }
}
