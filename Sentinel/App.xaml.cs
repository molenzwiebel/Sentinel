using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static Sentinel.Properties.Resources;

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
            // Setup start menu and COM server for notifications.
            StartMenuHelpers.Install(AppId, typeof(ActivationHandler).GUID);
            COMServerHelpers.Register(typeof(ActivationHandler).GUID);
            ActivationHandler.Initialize();

            // Start the application.
            _instance = new App();
            _instance.InitializeComponent();
            _instance.Run();
        }
        #endregion
    }
}
