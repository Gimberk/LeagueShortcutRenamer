using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;

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

            if (File.Exists(configFile)) {
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
            MessageBox.Show("You must set either the existing shortcut path or select the League executable path. If you're unsure, hover over the question marks for more information.",
                                "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        private void ShortcutBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            bool? success = dialog.ShowDialog();
            if (success == true)
            {
                string path = dialog.FileName;
                ShortcutPathTxt.Text = path;
            }
        }

        private void ExecBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
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

            if (!File.Exists(shortcut) && !File.Exists(exec))
            {
                ShowWarning();
                return;
            }

            File.WriteAllText(configFile, shortcut + "\n" + exec);

            saved = true;
            Hide();
        }

        private void Label_MouseEnter_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show(@"Select the shortcut file in your desktop. If you do not have one, leave this blank and fill in the next text box.",
                "Already have a shortcut?",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Label_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show(@"Select the LeagueClient.exe file. Usually found under C:\Riot Games\League of Legends\LeagueClient.exe",
                "Select this to have a shortcut automatically created",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
