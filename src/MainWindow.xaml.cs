using LSR.src;
using LSR.src.tools;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        public readonly bool DEBUG = false;

        ShortcutFormat currentFormat;
        Window1 selectPathWindow;

        public readonly string configFile;
        private bool running = false;
        private bool leagueRunning = false;

        public readonly DispatcherTimer leagueTimer = new DispatcherTimer();

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
            string pwd = Directory.GetCurrentDirectory();
            if (!Directory.Exists(pwd + @"\config")) Directory.CreateDirectory(pwd + @"\config");
            configFile = pwd + @"\config\paths.cfg";

            // initiate the dispatch timer to check for league
            leagueTimer.Interval = TimeSpan.FromMinutes(2);
            leagueTimer.Tick += TimerTick;
            leagueTimer.Start();

            // Run the initialization set ups
            this.Loaded += Main_Win_Loaded;
        }

        private void ClearWorkingDirectory()
        {
            Directory.Delete(Directory.GetCurrentDirectory() + @"\config", true);
        }

        private void TimerTick(object sender, EventArgs e) {
            leagueRunning = Utility.CheckLeagueRunning();
        }

        private void UpdatePathLbl() {
            if (new FileInfo(configFile).Length == 0) return;

            string path = File.ReadLines(configFile).ToArray()[0];
            PathLbl.Content = path;
        }

        private void Main_Win_Loaded(object sender, RoutedEventArgs e) {
            currentFormat = ShortcutFormat.None;

            selectPathWindow = new Window1(configFile)
            {
                Owner = this
            };

            if (!File.Exists(configFile)) File.Create(configFile);
            else UpdatePathLbl();
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
            }
        }

        private void SelectPaths_Click(object sender, RoutedEventArgs e) {
            selectPathWindow.ShowDialog();

            UpdatePathLbl();
        }

        private void EnableBtn_Click(object sender, RoutedEventArgs e)
        {
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

        private void DisableBtn_Click(object sender, RoutedEventArgs e)
        {
            running = false;
            EnableBtn.IsEnabled = true;
            DisableBtn.IsEnabled = false;

            LP.IsEnabled = true;
            MOTD.IsEnabled = true;
            None.IsEnabled = true;
        }
    }
}
