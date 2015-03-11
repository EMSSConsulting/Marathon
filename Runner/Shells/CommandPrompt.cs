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
            var startInfo = new ProcessStartInfo("cmd.exe", "/Q /C \"" + commandFile + "\"");

            return startInfo;
        }

        public override string PrepareCommands(IEnumerable<string> commands, bool failFast)
        {
            commands = commands.Select(x => x.Trim()).Where(x => x.Length > 0)
                .Select(x => string.Format("echo {1}{0}{1}", Environment.NewLine, x));
            commands = commands.Select(x => string.Format("{1}{0}if errorlevel 1 (exit /b %errorlevel%)", Environment.NewLine, x));
            return commands.Aggregate((left, right) => string.Format("{1}{0}{2}{0}", Environment.NewLine, left, right));
        }
    }
}
