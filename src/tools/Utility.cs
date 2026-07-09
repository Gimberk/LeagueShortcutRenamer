using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace LSR.src.tools
{
    public struct Version {
        public readonly int major, minor, revision;

        public Version(int major, int minor, int revision) {
            this.major      = major;
            this.minor      = minor;
            this.revision   = revision;
        }

        public override string ToString() {
            return $"{major}.{minor}.{revision}";
        }

        public static bool operator ==(Version a, Version b) {
            return a.Equals(b);
        }

        public static bool operator !=(Version a, Version b) {
            return !a.Equals(b);
        }

        public override bool Equals(object obj) {
            Version other = (Version)obj;
            return other.major == major && other.minor == minor && other.revision == revision;
        }

        public override int GetHashCode() {
            return HashCode.Combine(major, minor, revision);
        }
    }

    internal static class Utility
    {
        public static readonly string versionPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "League Shortcut Renamer", "version.json");

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

        public static async Task<Version> GetOnlineVersion() {
            string fileUrl = "https://raw.githubusercontent.com/Gimberk/LeagueShortcutRenamer/refs/heads/master/version.json";
            string destination = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try {
                await DownloadFile(fileUrl, destination);
                string jsonString = System.IO.File.ReadAllText(destination);
                string versionString = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)["version"];

                string[] ptr = versionString.ToString().Split('.');
                System.IO.File.Delete(destination);
                return new Version(int.Parse(ptr[0]), int.Parse(ptr[1]), int.Parse(ptr[2]));
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to check online version: {ex.Message}; aborting update check.");
                return new Version();
            }
        }

        public static void SaveVersion(Version v) {
            System.IO.File.WriteAllText(versionPath, $"{{ \"version\": \"{v.major}.{v.minor}.{v.revision}\" }}");
        }

        public static Version GetLocalVersion() {
            string jsonString = System.IO.File.ReadAllText(versionPath);
            string versionString = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)["version"];

            string[] ptr = versionString.ToString().Split('.');
            return new Version(int.Parse(ptr[0]), int.Parse(ptr[1]), int.Parse(ptr[2]));
        }

        public static async Task DownloadFile(string url, string destination) {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            Stream downloadStream = await response.Content.ReadAsStreamAsync();
            FileStream fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            await downloadStream.CopyToAsync(fileStream);

            // safely close open sockets to prevent memory leaks and access violations
            client.Dispose();
            response.Dispose();
            downloadStream.Close();
            fileStream.Close();
        }

        public static string GetShortcutTarget(string lnkFilePath) {
            if (!System.IO.File.Exists(lnkFilePath)) throw new FileNotFoundException($"Invalid .lnk file path: {lnkFilePath}");

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkFilePath);

            return shortcut.TargetPath;
        }
    }
}
