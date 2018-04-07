using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Sentinel.Properties.Resources;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

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
            icon = new NotifyIcon()
            {
                Text = "Sentinel",
                Icon = sentinel_icon,
                Visible = true,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Sentinel v1.0.0")
                    {
                        Enabled = false
                    },
                    new MenuItem("Settings", (sender, ev) =>
                    {
                        new AboutWindow(sentinel.settings).Show();
                    }),
                    new MenuItem("Quit", (a, b) => Shutdown())
                })
            };
            icon.Click += (a, b) => new AboutWindow(sentinel.settings).Show();

            if (!StartMenuHelpers.IsInstalled())
            {
                // We're not yet installed, show the wizard first.
                var wizard = new SetupWizard();
                wizard.Closed += (a, b) => sentinel = new Sentinel();
                wizard.Show();
            }
            else
            {
                // We're already installed, construct immediately.
                sentinel = new Sentinel();
            }
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
