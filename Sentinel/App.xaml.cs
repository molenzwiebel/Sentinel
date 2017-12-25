using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using static Sentinel.Properties.Resources;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Sentinel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public const string AppId = "nl.thijsmolendijk.sentinel";

        private Sentinel sentinel;
        private NotifyIcon icon;

        private App()
        {
            sentinel = new Sentinel();

            icon = new NotifyIcon()
            {
                Text = "Sentinel",
                Icon = sentinel_icon,
                Visible = true,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Sentinel v0.1")
                    {
                        Enabled = false
                    },
                    new MenuItem("Quit", (a, b) => Shutdown())
                })
            };
        }

        /**
         * Handles activation from a notification. Simply redirects this
         * to the local sentinel instance.
         */
        public void HandleActivation(string[] args, Dictionary<string, string> values)
        {
            sentinel.HandleActivation(args, values);
        }

        #region Startup & Singleton
        private static App _instance;
        public static App Instance => _instance;

        [STAThread]
        public static void Main()
        {
            if (!StartMenuHelpers.IsInstalled())
            {
                // Show initial box informing about start menu.
                if (MessageBox.Show(
                        "Welcome to Sentinel! It looks like this is your first time running Sentinel from this location. In order to properly display notifications, Sentinel needs to add itself to the start menu. Click OK to do this now, or click cancel to abort.",
                        "Sentinel - Setup", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    return;
                }

                // Install start menu and COM server.
                StartMenuHelpers.Install(AppId, typeof(ActivationHandler).GUID);
                COMServerHelpers.Register(typeof(ActivationHandler).GUID);

                // Ask if the user wants us to run in the background.
                if (MessageBox.Show(
                        "Sentinel runs in the background and will automatically interact with the League client. To make the process even smoother, Sentinel can optionally automatically start itself when you start your computer. Would you like to enable automatic starts?",
                        "Sentinel - Setup", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    regKey.SetValue(AppId, Process.GetCurrentProcess().MainModule.FileName);
                }

                MessageBox.Show(
                    "Sentinel is all ready to go! We will monitor your messages in the background and automatically interact with the League client, using very little resources in the process. Enjoy using Sentinel, and feel free to reach out if you encounter any problems!",
                    "Sentinel - Setup",
                    MessageBoxButtons.OK);
            }

            // Register our COM server.
            ActivationHandler.Initialize();

            // Start the application.
            _instance = new App();
            _instance.InitializeComponent();
            _instance.Run();
        }
        #endregion
    }
}
