using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Marathon
{
    public class Build
    {
        public Build(Runner runner, Models.BuildInfo buildInfo)
        {
            Runner = runner;
            BuildInfo = buildInfo;
        }

        private static Logger Log = LogManager.GetCurrentClassLogger();

        public Runner Runner { get; private set; }

        public Models.BuildInfo BuildInfo { get; private set; }

        #region Local Helpers

        protected string BuildsDirectory
        {
            get { return Path.GetFullPath(Runner.Configuration.Get("builds_path") ?? Path.Combine("tmp", "builds")); }
        }

        protected string ProjectDirectory
        {
            get { return Path.Combine(BuildsDirectory, "project-" + BuildInfo.ProjectID); }
        }

        protected string CommandFile
        {
            get { return Path.GetFullPath(Path.Combine(BuildsDirectory, "scripts", "project-" + BuildInfo.ProjectID + Runner.Shell.FileExtension)); }
        }

        protected bool RepositoryExists
        {
            get { return Directory.Exists(Path.Combine(ProjectDirectory, ".git")); }
        }

        #endregion

        public async Task<Models.BuildResult> PerformBuild()
        {
            Log.Trace("Preparing directories for build #{0}", BuildInfo.ID);
            PrepareDirectory();
            Log.Trace("Compiling command file for build #{0}", BuildInfo.ID);
            await BuildCommandFile();

            Log.Debug("Preparing process for build #{0}", BuildInfo.ID);
            var process = Runner.Shell.PrepareBuild(CommandFile, ProjectDirectory, BuildInfo);
            SetupEnvironment(process.StartInfo.EnvironmentVariables);

            Log.Trace("Running build #{0} process", BuildInfo.ID);
            var output = Runner.Shell.StartProcess(process);
#pragma warning disable 4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Task.Run(async () =>
#pragma warning restore 4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            {
                while (!process.HasExited)
                {
                    Log.Trace("Submitting build #{0} state to server", BuildInfo.ID);
                    var response = await Runner.Network.UpdateBuild(BuildInfo, BuildState.running, output.ToString());
                    if (response == NetworkResponse.Aborted) process.Kill();
                    await Task.Delay(5000);
                }
            });

            var result = await Runner.Shell.WaitForExit(process, BuildInfo.Timeout, () =>
            {
                process.Kill();
                output.AppendFormat("{0}CI Timeout. Execution tool longer than {1} seconds.{0}", Environment.NewLine, BuildInfo.Timeout);
            });

            Log.Trace("Build #{0} execution completed", BuildInfo.ID);

            Log.Trace("Cleaning up build #{0} files", BuildInfo.ID);
            Cleanup();

            Log.Debug("Build #{0} complete", BuildInfo.ID);
            return new Models.BuildResult() { Success = result, Output = output.ToString() };
        }

        protected void PrepareDirectory()
        {
            if (Directory.Exists(ProjectDirectory) && !BuildInfo.AllowGitFetch)
                Directory.Delete(ProjectDirectory, true);

            if (!Directory.Exists(ProjectDirectory))
                Directory.CreateDirectory(ProjectDirectory);

            if (!Directory.Exists(Path.GetDirectoryName(CommandFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(CommandFile));
        }

        protected async Task BuildCommandFile()
        {
            var commands = new List<string>();
            if (BuildInfo.AllowGitFetch && RepositoryExists)
            {
                commands.Add(string.Format("cd \"{0}\"", ProjectDirectory));
                commands.Add("git reset --hard");
                commands.Add("git clean -fdx");
                commands.Add(string.Format("git remote set-url origin {0}", BuildInfo.RepoURL));
                commands.Add("git fetch origin");
            }
            else
            {
                commands.Add(string.Format("cd \"{0}\"", BuildsDirectory));
                commands.Add(string.Format("git clone {0} project-{1}", BuildInfo.RepoURL, BuildInfo.ProjectID));
            }

            commands.Add(string.Format("cd \"{0}\"", ProjectDirectory));
            commands.Add("git reset --hard");
            commands.Add("git checkout " + BuildInfo.SHA);

            var setup = Runner.Configuration.GetSubKey("setup");
            foreach (var key in setup.GetSubKeys().Select(x => x.Key))
                commands.Add(setup.Get(key));

            commands.AddRange(BuildInfo.Commands.Split('\n').Select(x => x.Trim()));

            using (var commandFile = new StreamWriter(CommandFile))
                await commandFile.WriteAsync(Runner.Shell.PrepareCommands(commands, (Runner.Configuration.Get("fail_fast") ?? "false") == "true"));
        }

        protected void SetupEnvironment(StringDictionary environment)
        {
            var env = Runner.Configuration.GetSubKey("environment");
            foreach (var key in env.GetSubKeys().Select(x => x.Key))
                env[key] = env.Get(key);
        }

        protected void Cleanup()
        {
            if (File.Exists(CommandFile)) File.Delete(CommandFile);
        }
    }

    public enum BuildState
    {
        running,
        success,
        failed
    }
}
