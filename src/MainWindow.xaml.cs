using LSR.src;
using LSR.src.tools;
using System;
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
        ShortcutFormat currentFormat;
        Window1 selectPathWindow;

        public readonly string configFile;
        private bool running = false;
        private bool leagueRunning = false;

        public readonly DispatcherTimer leagueTimer = new DispatcherTimer();

        public MainWindow() {
            InitializeComponent();

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

        private void TimerTick(object sender, EventArgs e) {
            leagueRunning = Utility.CheckLeagueRunning();
        }

        private void UpdatePathLbl() {
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
            running = true;
            EnableBtn.IsEnabled = false;
            DisableBtn.IsEnabled = true;
        }

        private void DisableBtn_Click(object sender, RoutedEventArgs e)
        {
            running = false;
            EnableBtn.IsEnabled = true;
            DisableBtn.IsEnabled = false;
        }
    }
}
