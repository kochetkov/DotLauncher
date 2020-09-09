﻿using System.Diagnostics;

namespace DotLauncher.Utils
{
    internal static class ProcessUtils
    {
        public static void StartSilentAndWait(string filename, string arguments = "")
        {
            var startInfo = new ProcessStartInfo(filename, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo)?.WaitForExit();
        }
    }
}