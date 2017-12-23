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
        public const string APP_ID = "nl.thijsmolendijk.sentinel";

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
        private static App instance;
        public static App Instance
        {
            get { return instance; }
        }

        [STAThread]
        public static void Main()
        {
            // Setup start menu and COM server for notifications.
            StartMenuHelpers.Install(APP_ID, typeof(ActivationHandler).GUID);
            COMServerHelpers.Register(typeof(ActivationHandler).GUID);
            ActivationHandler.Initialize();

            // Start the application.
            instance = new App();
            instance.InitializeComponent();
            instance.Run();
        }
        #endregion
    }
}
