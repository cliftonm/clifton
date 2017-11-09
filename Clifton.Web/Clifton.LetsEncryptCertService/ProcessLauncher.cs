using System;
using System.Diagnostics;

namespace Clifton.LetsEncryptCertService
{
    public static class ProcessLauncher
    {
        public static Process LaunchProcess(string processName, string arguments, Action<string> onOutput, Action<string> onError = null)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.FileName = processName;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += (sndr, args) => { if (args.Data != null) onOutput(args.Data); };

            if (onError != null)
            {
                p.ErrorDataReceived += (sndr, args) => { if (args.Data != null) onError(args.Data); };
            }

            p.Start();

            // Interestingly, this has to be called after Start().
            p.EnableRaisingEvents = true;
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }
    }
}
