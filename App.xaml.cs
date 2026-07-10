using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LSR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() {
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString());
                e.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show(e.ExceptionObject.ToString());
            };
        }

        private static Mutex mutex = null;
        private const string UniqueAppName = "Global\\LSR";

        #region Expose Function Headers

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #endregion

        private const int SW_RESTORE = 0;

        // avoid creating a new instance of this application when it's already running
        protected override void OnStartup(StartupEventArgs e) {
            bool createdNew;
            mutex = new Mutex(true, UniqueAppName, out createdNew);

            if (!createdNew) {
                ActivatePreviousInstance();
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        // locate the previous existing process
        private void ActivatePreviousInstance() {
            IntPtr handle = FindWindow(null, "League Shortcut Renamer");
            if (handle != IntPtr.Zero) {
                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);
            }
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);

            if (this.MainWindow != null) {
                if (this.MainWindow.WindowState == WindowState.Minimized) this.MainWindow.WindowState = WindowState.Normal;

                // immediately bring to front
                this.MainWindow.Topmost = true;
                this.MainWindow.Topmost = false;
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}