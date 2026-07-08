using LSR.src.tools;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LSR.src {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {
        private static bool saved = false;

        private readonly string configFile;
        private readonly string playerFile;

        public Window1(string cfg, string playerInfo) {
            InitializeComponent();

            configFile  = cfg;
            playerFile  = playerInfo;

            if (File.Exists(configFile) && new FileInfo(configFile).Length > 0) {
                string[] lines = File.ReadAllLines(configFile).ToArray();

                ShortcutPathTxt.Text    = lines[0];
                ExecPathTxt.Text        = lines[1];
            }

            if (File.Exists(playerFile) && new FileInfo(playerFile).Length > 0) {
                string[] lines = File.ReadAllLines(playerFile).ToArray();

                if (lines[0] == "True") {
                    UserTxt.Text = lines[1];
                    TagTxt.Text = lines[2];
                    APIKeyTxt.Text = lines[3];
                }
            }
        }

        // Handle to Hide instead of closing the window as there's only ever one instance of this window
        private void SelectShortcutPath_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;

            /*
            if (!saved)
            {
                ShowWarning();
                return;
            }
            */

            this.Hide();
        }

        private void ShowWarning() {
            MessageBox.Show("You must set either the existing shortcut path or select the League executable path and hit \"Create\". If you're unsure, hover over the question marks for more information.",
                                "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShortcutBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Shortcuts (*.lnk)|*.lnk",
                DereferenceLinks = false
            };

            bool? success = dialog.ShowDialog();
            if (success == true)
            {
                string path = dialog.FileName;
                ShortcutPathTxt.Text = path;
            }
        }

        private void ExecBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "Executables (*.exe)|*.exe"
            };

            bool? success = dialog.ShowDialog();
            if (success == true)
            {
                string path = dialog.FileName;
                ExecPathTxt.Text = path;
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string shortcut = ShortcutPathTxt.Text;
            string exec = ExecPathTxt.Text;

            if (!File.Exists(shortcut)) {
                ShowWarning();
                return;
            }

            bool emptyPlayerInfo = false;
            if (APIKeyTxt.Text == "API Key") {
                emptyPlayerInfo = true;
                MessageBoxResult result = MessageBox.Show("Some of your account information is missing or you've failed to provide the API Key. The option for your Current LP Will be disabled until your information is verified. Would you like to continue?",
                    "Missing Information", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;
            }
            if (UserTxt.Text == "League Username" && !emptyPlayerInfo) {
                emptyPlayerInfo = true;
                MessageBoxResult result = MessageBox.Show("Some of your account information is missing or you've failed to provide the API Key. The option for your Current LP Will be disabled until your information is verified. Would you like to continue?",
                    "Missing Information", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;
            }
            if (TagTxt.Text == "Tagline" && !emptyPlayerInfo) {
                emptyPlayerInfo = true;
                MessageBoxResult result = MessageBox.Show("Some of your account information is missing or you've failed to provide the API Key. The option for your Current LP Will be disabled until your information is verified. Would you like to continue?",
                    "Missing Information", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;
            }

            string puuid = string.Empty;
            if (!emptyPlayerInfo) {
                try {
                    RiotAccountService apiServicer = new RiotAccountService(APIKeyTxt.Text);
                    string attemptedPUUID = await apiServicer.GetPUUID("Americas", UserTxt.Text, TagTxt.Text);
                    if (attemptedPUUID.ToCharArray()[0] == 'e') {
                        if (MessageBox.Show("Invalid UserID, Tagline, or API Key. Ensure they're correct. Would you like to continue regardless?", "Save Failure", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
                    }
                    else {
                        puuid = attemptedPUUID;
                        MessageBox.Show("Successfully connected Riot account.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch {
                    if (MessageBox.Show("Invalid UserID, Tagline, or API Key. Ensure they're correct. Would you like to continue regardless?", "Save Failure", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
                }
            }

            File.WriteAllText(configFile, shortcut + "\n" + exec + "\n" + "True");
            File.WriteAllText(playerFile, (puuid != string.Empty).ToString() + "\n" + UserTxt.Text + "\n" + TagTxt.Text + "\n" + APIKeyTxt.Text + (puuid != string.Empty ? "\n" + puuid : ""));

            saved = true;
            Hide();
        }

        private void Label_MouseEnter_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show("Select the shortcut file in your desktop. If you do not have one, leave this blank and fill in the next text box.",
                "Already have a shortcut?",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Label_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show(@"Select the RiotClientServices.exe file. Usually found under C:\Riot Games\Riot Client\RiotClientServices.exe",
                "Select this to have a shortcut automatically created",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
           if (!File.Exists(ExecPathTxt.Text)) {
                MessageBox.Show("Invalid League of Legends executable path.", "Creation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
           }

            const string shortcutName = "LeagueOfCustom";

            Exception res = Utility.CreateLeagueDesktopShortcut(ExecPathTxt.Text, shortcutName);
            if (res != null) MessageBox.Show($"Failed to create shortcut: {res.Message}", "Creation Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            else {
                MessageBox.Show("Successfully created shortcut on desktop!", "Creation Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ShortcutPathTxt.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), shortcutName + ".lnk");
            }
        }

        private void DetectBtn_Click(object sender, RoutedEventArgs e) {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string[] files = Directory.GetFiles(desktop);

            foreach (var f in files) {
                if (new FileInfo(f).Extension != ".lnk") continue;

                string target = Utility.GetShortcutTarget(f);
                if (new FileInfo(target).Name == "RiotClientServices.exe") {
                    MessageBox.Show("Successfully found League of Legends shortcut!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShortcutPathTxt.Text = f;
                    break;
                }
            }
        }

        private void TextBoxContentChanged(object sender, TextChangedEventArgs e) {
            if (sender is TextBox input) {
                if (input.Text != string.Empty) return;
                switch (input.Name) {
                    case "UserTxt":
                        input.Text = "League Username";
                        break;
                    case "TagTxt":
                        input.Text = "Tagline";
                        break;
                    case "APIKeyTxt":
                        input.Text = "API Key";
                        break;
                    case "ShortcutPathTxt":
                        input.Text = "League Shortcut Path";
                        break;
                    case "ExecPathTxt":
                        input.Text = "League Executable Path";
                        break;
                }
            }
        }
    }
}