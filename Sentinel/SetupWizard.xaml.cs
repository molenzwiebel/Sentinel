using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace Sentinel
{
    enum SetupWizardState
    {
        StartMenu,
        Startup,
        Done
    }

    /// <summary>
    /// Interaction logic for SetupWizard.xaml
    /// </summary>
    public partial class SetupWizard : Window
    {
        private SetupWizardState state = SetupWizardState.StartMenu;

        public SetupWizard()
        {
            InitializeComponent();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (state == SetupWizardState.StartMenu)
            {
                Application.Current.Shutdown();
            } else if (state == SetupWizardState.Startup)
            {
                state = SetupWizardState.Done;
                Render();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (state == SetupWizardState.StartMenu)
            {
                // Install start menu and COM server.
                StartMenuHelpers.Install(App.AppId, typeof(ActivationHandler).GUID);
                COMServerHelpers.Register(typeof(ActivationHandler).GUID);

                state = SetupWizardState.Startup;
                Render();
            } else if (state == SetupWizardState.Startup)
            {
                var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                regKey.SetValue(App.AppId, Process.GetCurrentProcess().MainModule.FileName);

                state = SetupWizardState.Done;
                Render();
            } else if (state == SetupWizardState.Done)
            {
                Close();
            }
        }

        private void Render()
        {
            if (state == SetupWizardState.Startup)
            {
                title.Content = "Automatic Start";
                textContents.Text = @"Sentinel runs in the background and will automatically interact with the League client. To make the process even smoother, Sentinel can optionally automatically start itself when you start your computer. Would you like to enable automatic starts?";
                stopButton.Content = "Skip";
                nextButton.Content = "Add To Startup";
            }

            if (state == SetupWizardState.Done)
            {
                title.Content = "Done!";
                textContents.Text = @"Sentinel is all ready to go! We will monitor your messages in the background and automatically interact with the League client, using very little resources in the process. Enjoy using Sentinel, and feel free to reach out if you encounter any problems!";
                stopButton.Visibility = Visibility.Hidden;
                nextButton.Content = "Start Using Sentinel";
            }
        }
    }
}
