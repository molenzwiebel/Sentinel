using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

            AllChatsCheckbox.IsChecked = Settings.ShowAllConversations;
            InGameCheckbox.IsChecked = Settings.ShowWhileIngame;

            AllChatsCheckbox.Checked += (a, b) =>
            {
                Settings.ShowAllConversations = AllChatsCheckbox.IsChecked.Value;
                WriteSettings();
            };

            InGameCheckbox.Checked += (a, b) =>
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
