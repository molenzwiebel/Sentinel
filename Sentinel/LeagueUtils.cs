using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Sentinel
{
    /**
     * Some static utilities used to interact/query the state of the league client process.
     */
    static class LeagueUtils
    {
        private static Regex AUTH_TOKEN_REGEX = new Regex("\"--remoting-auth-token=(.+?)\"");
        private static Regex PORT_REGEX = new Regex("\"--app-port=(\\d+?)\"");

        /**
         * Returns a tuple with the PID, remoting auth token and port of the current league client.
         * Returns null if the current league client is not running.
         */
        public static Tuple<int, string, string> GetLeagueStatus()
        {
            // Find the LeagueClientUx process.
            foreach (Process p in Process.GetProcessesByName("LeagueClientUx"))
            {
                // Use WMI to figure out its command line.
                using (ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + p.Id.ToString()))
                using (ManagementObjectCollection moc = mos.Get())
                {
                    string commandLine = (string)moc.OfType<ManagementObject>().First()["CommandLine"];

                    // Use regex to extract data, return it.
                    return new Tuple<int, string, string>(
                        p.Id,
                        AUTH_TOKEN_REGEX.Match(commandLine).Groups[1].Value,
                        PORT_REGEX.Match(commandLine).Groups[1].Value
                    );
                }
            }

            // LeagueClientUx process was not found. Return null.
            return null;
        }

        /**
         * Checks if any window created by the specified pid is active. If yes, returns true.
         */
        public static bool IsWindowFocused(int pid)
        {
            var handle = GetForegroundWindow();
            if (handle.ToInt32() == 0) return false;

            int focusedPid;
            GetWindowThreadProcessId(handle, out focusedPid);

            return focusedPid == pid;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
    }
}
