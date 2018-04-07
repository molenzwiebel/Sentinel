using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static Sentinel.Properties.Resources;

namespace Sentinel
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private SentinelSettings Settings;

        public AboutWindow(SentinelSettings settings)
        {
            InitializeComponent();

            Settings = settings;
            Logo.Source = Imaging.CreateBitmapSourceFromHIcon(sentinel_icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            AllChatsCheckbox.IsChecked = Settings.ShowAllConversations;
            InGameCheckbox.IsChecked = Settings.ShowWhileIngame;

            AllChatsCheckbox.Click += (a, b) =>
            {
                Settings.ShowAllConversations = AllChatsCheckbox.IsChecked.Value;
                WriteSettings();
            };

            InGameCheckbox.Click += (a, b) =>
            {
                Settings.ShowWhileIngame = InGameCheckbox.IsChecked.Value;
                WriteSettings();
            };
        }

        /**
         * Opens the project github link in the default browser.
         */
        public void OpenGithub(object sender, EventArgs args)
        {
            Process.Start("https://github.com/molenzwiebel/sentinel");
        }

        /**
         * Opens the project discord in the default browser.
         */
        public void OpenDiscord(object sender, EventArgs args)
        {
            Process.Start("https://discord.gg/bfxdsRC");
        }

        /**
         * (Attempts to) uninstall sentinel.
         */
        public void Uninstall(object sender, EventArgs args)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to uninstall Sentinel? All files will be deleted.", "Sentinel", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            // Step 1: Delete AppData.
            try { Directory.Delete(Sentinel.StorageDir, true); } catch { /* ignored */ }

            // Step 2: Delete Executable.
            Process.Start(new ProcessStartInfo
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + System.Windows.Forms.Application.ExecutablePath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });

            // Step 3: Stop Program.
            Application.Current.Shutdown();
        }

        /**
         * Writes the updated settings to file.
         */
        private void WriteSettings()
        {
            using (Stream stream = File.Open(Sentinel.SettingsPath, FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, Settings);
            }
        }
    }
}
