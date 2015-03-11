using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Shells
{
    public class PowerShell : ShellBase
    {
        public override string Name { get { return "powershell"; } }

        public override string FileExtension
        {
            get { return ".ps1"; }
        }

        protected override ProcessStartInfo PrepareProcess(string commandFile)
        {
            var startInfo = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + commandFile + "\"");

            return startInfo;
        }
        
        public override string PrepareCommands(IEnumerable<string> commands, bool failFast)
        {
            commands = commands.Select(x => x.Trim()).Where(x => x.Length > 0)
                .Select(x => string.Format("Write {1}{0}{1}", Environment.NewLine, x.Replace("\"", "\"\"")));
            commands = commands.Select(x => string.Format("{1}{0}if (-not $?) {{ Exit $LastExitCode }}", Environment.NewLine, x));
            return commands.Aggregate((left, right) => string.Format("{1}{0}{2}{0}", Environment.NewLine, left, right));
        }
    }
}
