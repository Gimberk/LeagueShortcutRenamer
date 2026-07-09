using LSR.src;
using LSR.src.tools;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LSR {
    enum ShortcutFormat {
        None, LP, MOTD
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public static readonly src.tools.Version installedVersion = new src.tools.Version(0,2,0);

        public readonly bool DEBUG = false;

        ShortcutFormat currentFormat;
        Window1 selectPathWindow;

        public readonly string configFile;
        public readonly string playerFile;
        public readonly string startupShortcut;

        private bool running = false;
        private bool leagueRunning = false;

        public readonly DispatcherTimer leagueTimer = new DispatcherTimer();
        public readonly DispatcherTimer rankTimer = new DispatcherTimer();

        #region Instance Management

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private bool isExplicitClose = false;

        protected override void OnStateChanged(EventArgs e) {
            // remove the icon from the task bar
            if (this.WindowState == WindowState.Minimized) this.ShowInTaskbar = false;

            base.OnStateChanged(e);
        }

        private void InitializeNotifyIcon() {
            notifyIcon = new System.Windows.Forms.NotifyIcon();

            notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            notifyIcon.Text = "League Shortcut Renamer";
            notifyIcon.Visible = true;

            notifyIcon.Click += (s, args) => RestoreWindow();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, args) => RestoreWindow());
            contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());
        }

        private void RestoreWindow() {
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // intercept close and minimize instead
        protected override void OnClosing(CancelEventArgs e) {
            if (MessageBox.Show("Would you Like to hide the application rather than close it? Hiding it will keep it running in the background. It will not function when closed and will be automatically disabled.", "Hide on close?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                isExplicitClose = true;

            if (!isExplicitClose) {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
            else {
                notifyIcon.Dispose();
                base.OnClosing(e);
            }
        }

        private void ExitApplication() {
            isExplicitClose = true;
            Close();
        }

        #endregion

        public MainWindow() {
            InitializeComponent();

            if (DEBUG) ClearWorkingDirectory();

            InitializeNotifyIcon();

            // locate or create the config file for paths
            string parentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "League Shortcut Renamer");

            configFile = parentFolder + @"\config\paths.cfg";
            playerFile = parentFolder + @"\config\player.cfg";
            startupShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LeagueShortcutRenamer.lnk");

            if (!Directory.Exists(parentFolder + @"\config")) {
                Directory.CreateDirectory(parentFolder + @"\config");

                MessageBoxResult result = MessageBox.Show("Would you like to allow League Shortcut Renamer to open when your Windows machine starts?", "Make startup app?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) MakeStartupApp();
            }

            // initiate the dispatch timer to check for league
            leagueTimer.Interval = TimeSpan.FromMinutes(2);
            leagueTimer.Tick += TimerTick;
            leagueTimer.Start();
            TimerTick(null, null);

            // initiate the dispatch timer to fetch player rank when league is open
            rankTimer.Interval = TimeSpan.FromMinutes(1);
            rankTimer.Tick += RankTimerTick;
            rankTimer.Start();

            // Run the initialization set ups
            this.Loaded += Main_Win_Loaded;
        }

        private void MakeStartupApp() {
            string exe = Process.GetCurrentProcess().MainModule.FileName;

            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(startupShortcut);

            shortcut.TargetPath = exe;
            shortcut.Save();
        }

        private void TimerTick(object sender, EventArgs e) { leagueRunning = Utility.CheckLeagueRunning(); }

        private async void RankTimerTick(object sender, EventArgs e) {
            // reduce API requests per minute by only querying if league is open
            if (!leagueRunning || !running || currentFormat != ShortcutFormat.LP) return;

            LeagueEntryDTO account = await GetPlayerRank();
            string shortcut = GetShortcutPath();
            string name = account.Tier == "SILVER" ? $"Still silver {account.Rank} huh... Get to grinding boi!" : $"{account.Tier} {account.Rank} - {account.LeaguePoints} LP";
            string newShortcut = Path.Combine(Path.GetDirectoryName(shortcut), name + ".lnk");

            File.Move(shortcut, newShortcut);

            string[] lines = File.ReadAllLines(configFile);
            lines[0] = newShortcut;
            File.WriteAllLines(configFile, lines);
            UpdatePathLbl();
        }


        private string GetShortcutPath() {
            if (File.Exists(configFile) && new FileInfo(configFile).Length > 0 && File.ReadAllLines(configFile).Length == 3)
                return File.ReadAllLines(configFile)[0];
            throw new InvalidDataException("Must have valid shortcut path to enable LSR");
        }

        private bool IsPlayerInfoValid() {
            if (!File.Exists(playerFile) || new FileInfo(playerFile).Length == 0) return false;
            return File.ReadAllLines(playerFile)[0] == "True";
        }

        private string GetPUUID() {
            return IsPlayerInfoValid() ? File.ReadAllLines(playerFile)[4] : string.Empty;
        }

        private string GetAPIKey() {
            return IsPlayerInfoValid() ? File.ReadAllLines(playerFile)[3] : string.Empty;
        }

        private async Task<LeagueEntryDTO> GetPlayerRank() {
            if (!IsPlayerInfoValid()) throw new InvalidDataException("Must Have Valid Player Info to Access Rank");

            RiotAccountService accountServicer = new RiotAccountService(GetAPIKey());
            string puuid = GetPUUID();
            LeagueEntryDTO rankFormatted = await accountServicer.GetRank(puuid);

            return rankFormatted;
        }

        private void ShowLegalBoilerplate() {
            MessageBox.Show("League Shortcut Renamer is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc", "Legal Jibber Jabber", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearWorkingDirectory() {
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "League Shortcut Renamer") + @"\config", true);
            if (File.Exists(startupShortcut)) File.Delete(startupShortcut);
        }

        private void UpdatePathLbl() {
            if (new FileInfo(configFile).Length == 0) return;

            string path = File.ReadAllLines(configFile).ToArray()[0];
            PathLbl.Content = path;
        }

        private void UpdatePlayerLbl() {
            bool valid = IsPlayerInfoValid();
            LP.IsEnabled = valid;
            SignedInLbl.Content = valid ? $"Signed in as: {File.ReadAllLines(playerFile)[1]}#{File.ReadAllLines(playerFile)[2]}" : string.Empty;
        }

        private async void Main_Win_Loaded(object sender, RoutedEventArgs e) {
            currentFormat = ShortcutFormat.None;

            LP.IsEnabled = IsPlayerInfoValid();

            selectPathWindow = new Window1(configFile, playerFile)
            {
                Owner = this
            };

            EnableBtn.IsEnabled = (currentFormat != ShortcutFormat.None && File.Exists(configFile) && new FileInfo(configFile).Length > 0 && File.ReadAllLines(configFile).Length == 3);

            if (!File.Exists(configFile)) File.Create(configFile);
            else UpdatePathLbl();

            if (!File.Exists(playerFile)) File.Create(playerFile);
            UpdatePlayerLbl();

            if (!File.Exists(Utility.versionPath)) Utility.SaveVersion(installedVersion);
            else {
                src.tools.Version online = await Utility.GetOnlineVersion(), local = Utility.GetLocalVersion();
                if (online != local) {
                    MessageBoxResult result =
                        MessageBox.Show($"There is a new version of League Shortcut Renamer Available:\n\tLocal Version: {local};\n\tNewest Version: {online}", "Update LSR?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes) {

                    }
                }
            }
        }

        private void Current_Checked(object sender, RoutedEventArgs e) {
            if (sender is RadioButton button) {
                switch (button.Name) {
                    case "LP":
                        currentFormat = ShortcutFormat.LP;
                        break;
                    case "MOTD":
                        currentFormat = ShortcutFormat.MOTD;
                        break;
                    case "None":
                        currentFormat = ShortcutFormat.None;
                        break;
                    default:
                        MessageBox.Show("Tried to set an invalid shortcut format state; choose one of: NONE, LP, MOTD",
                            "Invalid Format State",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        currentFormat = ShortcutFormat.None;
                        break;
                }

                EnableBtn.IsEnabled = (currentFormat != ShortcutFormat.None && File.Exists(configFile) && new FileInfo(configFile).Length > 0 && File.ReadAllLines(configFile).Length == 3);
            }
        }

        private void SelectPaths_Click(object sender, RoutedEventArgs e) {
            selectPathWindow.ShowDialog();

            UpdatePathLbl();
            UpdatePlayerLbl();
            EnableBtn.IsEnabled = (File.Exists(configFile) && new FileInfo(configFile).Length > 0 && File.ReadAllLines(configFile)[2] == "True");
        }

        private void EnableBtn_Click(object sender, RoutedEventArgs e) {
            if (PathLbl.Content.ToString().Trim() == "None") {
                MessageBox.Show("Cannot enable LSR: Missing account and/or shortcut path; visit the necessary settings to update this.",
                    "Start Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentFormat == ShortcutFormat.None) {
                MessageBox.Show("Cannot enable LSR: Must select a valid format.", "Start Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            running = true;
            EnableBtn.IsEnabled = false;
            DisableBtn.IsEnabled = true;

            LP.IsEnabled = false;
            MOTD.IsEnabled = false;
            None.IsEnabled = false;
        }

        private void DisableBtn_Click(object sender, RoutedEventArgs e) {
            running = false;
            EnableBtn.IsEnabled = true;
            DisableBtn.IsEnabled = false;

            LP.IsEnabled = true;
            MOTD.IsEnabled = false;
            None.IsEnabled = true;
        }

        private async void ShowLegalBtn_Click(object sender, RoutedEventArgs e) {
            ShowLegalBoilerplate();
        }
    }
}
