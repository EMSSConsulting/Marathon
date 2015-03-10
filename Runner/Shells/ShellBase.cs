using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Shells
{
    public abstract class ShellBase
    {
        protected static List<ShellBase> RegisteredShells { get; private set; } = new List<ShellBase>();

        public static void Register(params ShellBase[] shells)
        {
            RegisteredShells.AddRange(shells.Where(x => x != null));
        }

        public static ShellBase GetShell(string name)
        {
            foreach (var shell in RegisteredShells)
                if (shell.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) return shell;
            return RegisteredShells.FirstOrDefault();
        }

        public abstract string Name { get; }

        public abstract string FileExtension { get; }

        public abstract string PrepareCommands(IEnumerable<string> commands);

        protected abstract ProcessStartInfo PrepareProcess(string commandFile);

        public virtual Process PrepareBuild(string commandFile, string workingDirectory, Models.BuildInfo buildInfo)
        {
            var process = new Process();
            process.StartInfo = PrepareProcess(commandFile);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = workingDirectory;

            process.StartInfo.EnvironmentVariables["BUNDLE_GEMFILE"] = Path.Combine(workingDirectory, "Gemfile");
            process.StartInfo.EnvironmentVariables["BUNDLE_BIN_PATH"] = "";
            process.StartInfo.EnvironmentVariables["RUBYOPT"] = "";
            
            process.StartInfo.EnvironmentVariables["CI_SERVER"] = "yes";
            process.StartInfo.EnvironmentVariables["CI_SERVER_NAME"] = "GitLab CI";
            process.StartInfo.EnvironmentVariables["CI_SERVER_VERSION"] = "";
            process.StartInfo.EnvironmentVariables["CI_SERVER_REVISION"] = "";

            process.StartInfo.EnvironmentVariables["CI_BUILD_REF"] = buildInfo.Ref;
            process.StartInfo.EnvironmentVariables["CI_BUILD_REF_NAME"] = buildInfo.Ref;
            process.StartInfo.EnvironmentVariables["CI_BUILD_ID"] = buildInfo.ID.ToString();

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            return process;
        }

        public virtual StringBuilder StartProcess(Process process)
        {
            var output = new StringBuilder();

            DataReceivedEventHandler onData = (o, e) =>
            {
                output.AppendLine(e.Data);
            };

            process.OutputDataReceived += onData;
            process.ErrorDataReceived += onData;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return output;
        }

        public virtual async Task<bool> WaitForExit(Process process, int timeout, Action onTimeout = null)
        {
            await Task.Run(() =>
            {
                if (!process.WaitForExit(timeout * 1000))
                {
                    if (onTimeout != null) onTimeout();
                    else process.Kill();
                }
            });

            return process.ExitCode == 0;
        }
    }
}
