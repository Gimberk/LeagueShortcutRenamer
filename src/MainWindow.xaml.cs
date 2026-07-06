using LSR.src;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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

        public MainWindow() {
            InitializeComponent();

            string pwd = Directory.GetCurrentDirectory();
            if (!Directory.Exists(pwd + @"\config")) Directory.CreateDirectory(pwd + @"\config");
            Console.WriteLine(pwd + @"\config");

            // Run the initialization set ups
            this.Loaded += Main_Win_Loaded;
        }

        private void Main_Win_Loaded(object sender, RoutedEventArgs e) {
            currentFormat = ShortcutFormat.None;

            selectPathWindow = new Window1
            {
                Owner = this
            };
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
        }
    }
}
