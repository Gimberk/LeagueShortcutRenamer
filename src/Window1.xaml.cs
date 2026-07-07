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

        public Window1(string cfg) {
            InitializeComponent();

            configFile = cfg;

            if (File.Exists(configFile) && new FileInfo(configFile).Length > 0) {
                string[] lines = File.ReadLines(configFile).ToArray();
                ShortcutPathTxt.Text = lines[0];
                ExecPathTxt.Text = lines[1];
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

        private void ShowWarning()
        {
            MessageBox.Show("You must set either the existing shortcut path or select the League executable path and hit \"Create\". If you're unsure, hover over the question marks for more information.",
                                "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string shortcut = ShortcutPathTxt.Text;
            string exec = ExecPathTxt.Text;

            if (!File.Exists(shortcut)) {
                ShowWarning();
                return;
            }


            File.WriteAllText(configFile, shortcut + "\n" + exec);

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
    }
}