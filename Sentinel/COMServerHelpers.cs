using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sentinel
{
    /**
     * Helper functions for registering/handling the COM server protocol. The COM
     * server is needed to respond to activation events from the events.
     */
    public static class COMServerHelpers
    {
        /**
         * Registers a new COM server for the specified handler guid.
         */
        public static void Register(Guid handler)
        {
            var registryPath = String.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", handler);
            var exePath = Process.GetCurrentProcess().MainModule.FileName;

            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(registryPath);
            key.SetValue(null, exePath);
        }

        #region Windows 10 Notification APIS
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct NotificationData
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Key;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Value;
        }

        [ComImport, Guid("53E31837-6600-4A81-9395-75CFFE746F94"), ComVisible(true), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INotificationActivationCallback
        {
            void Activate(
                [In, MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
                [In, MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] NotificationData[] data,
                [In, MarshalAs(UnmanagedType.U4)] uint dataCount
            );
        }
        #endregion
    }
}
