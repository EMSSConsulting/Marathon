using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Shells
{
    public class CommandPrompt : ShellBase
    {
        public override string Name { get { return "cmd"; } }

        public override string FileExtension
        {
            get { return ".bat"; }
        }

        protected override ProcessStartInfo PrepareProcess(string commandFile)
        {
            var startInfo = new ProcessStartInfo("cmd.exe", $"/Q /C \"{commandFile}\"");
            
            return startInfo;
        }

        public override string PrepareCommands(IEnumerable<string> commands)
        {
            return commands.Select(x => x.Trim()).Where(x => x.Length > 0)
                .Select(x => $"echo {x}{Environment.NewLine}{x}")
                //.Select(x => $"{x}{Environment.NewLine}if errorlevel 1 (exit /b %errorlevel%)")
                .Aggregate((left, right) => $"{left}{Environment.NewLine}{right}{Environment.NewLine}");
        }
    }
}
