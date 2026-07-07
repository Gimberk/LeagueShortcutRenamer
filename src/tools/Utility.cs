using System;
using System.Diagnostics;

namespace LSR.src.tools
{
    internal static class Utility
    {
        public static bool CheckLeagueRunning()
        {
            var leagueProcess = Process.GetProcessesByName("LeagueClient");
            return leagueProcess.Length > 0;
        }
    }
}
