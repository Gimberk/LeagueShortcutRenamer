using System;
using System.Diagnostics;
using IWshRuntimeLibrary;
using System.IO;

namespace LSR.src.tools
{
    internal static class Utility
    {
        /// <summary>
        /// Compiles the list of active processes and checks for LeagueClient
        /// </summary>
        /// <returns>True if running, false if inactive</returns>
        public static bool CheckLeagueRunning() {
            var leagueProcess = Process.GetProcessesByName("LeagueClient");
            return leagueProcess.Length > 0;
        }

        /// <summary>
        /// Creates a shortcut in the desktop directory for the host pointing to League Of legends
        /// </summary>
        /// <param name="execPath">The path for the LeagueOfLegends.exe</param>
        /// <returns>An exception on fail, null on success</returns>
        public static Exception CreateLeagueDesktopShortcut(string execPath, string name) {
            try
            {
                string desktopFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutLocation = Path.Combine(desktopFolderPath, name + ".lnk");

                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

                shortcut.TargetPath = execPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(execPath);
                shortcut.Description = "Created by LSR";
                shortcut.IconLocation = execPath + ",0";

                shortcut.Save();
                return null;
            }
            catch (Exception ex) { return ex; }
        }

        public static string GetShortcutTarget(string lnkFilePath) {
            if (!System.IO.File.Exists(lnkFilePath)) throw new FileNotFoundException($"Invalid .lnk file path: {lnkFilePath}");

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkFilePath);

            return shortcut.TargetPath;
        }
    }
}
