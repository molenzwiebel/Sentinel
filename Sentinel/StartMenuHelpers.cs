using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sentinel
{
    /**
     * We need to be added to the start menu in order for notifications to properly show up.
     * This class contains some helpers to register ourself using COM apis (which are horrible).
     */
    static class StartMenuHelpers
    {
        private static string SHORTCUT_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\Sentinel.lnk";

        /**
         * Adds a new shortcut for the current program to the start menu, if it doesn't exist yet.
         */
        public static void Install(string appId, Guid handler)
        {
            if (!File.Exists(SHORTCUT_PATH))
            {
                // Find the path to the current executable
                String exePath = Process.GetCurrentProcess().MainModule.FileName;
                InstallShortcut(exePath, appId, handler);
            }
        }

        /**
         * Adds a new shortcut for the specified executable and app id.
         */
        public static void InstallShortcut(string exePath, string appId, Guid handler)
        {
            // Start a new shortcut object for our new shortcut.
            IShellLinkW newShortcut = (IShellLinkW) new CShellLink();
            newShortcut.SetPath(exePath);

            // Set our classid and handler GUID for the shortcut.
            IPropertyStore newShortcutProperties = (IPropertyStore) newShortcut;

            PropVariantHelper varAppId = new PropVariantHelper();
            varAppId.SetValue(appId);
            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ID, varAppId.Propvariant);

            PropVariantHelper varToastId = new PropVariantHelper();
            varToastId.VarType = VarEnum.VT_CLSID;
            varToastId.SetValue(handler);
            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ToastActivatorCLSID, varToastId.Propvariant);

            // Save the shortcut.
            IPersistFile newShortcutSave = (IPersistFile) newShortcut;
            newShortcutSave.Save(SHORTCUT_PATH, true);
        }

        #region COM APIs
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellLinkW
        {
            void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotKey(out short wHotKey);
            void SetHotKey(short wHotKey);
            void GetShowCmd(out uint iShowCmd);
            void SetShowCmd(uint iShowCmd);
            void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] out StringBuilder pszIconPath, int cchIconPath, out int iIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistFile
        {
            void GetCurFile([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.U4)] long dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PROPVARIANT
        {
            [FieldOffset(0)]
            public ushort vt;
            [FieldOffset(8)]
            public IntPtr unionmember;
            [FieldOffset(8)]
            public UInt64 forceStructToLargeEnoughSize;
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPropertyStore
        {
            void GetCount([Out] out uint propertyCount);
            void GetAt([In] uint propertyIndex, [Out, MarshalAs(UnmanagedType.Struct)] out PROPERTYKEY key);
            void GetValue([In, MarshalAs(UnmanagedType.Struct)] ref PROPERTYKEY key, [Out, MarshalAs(UnmanagedType.Struct)] out PROPVARIANT pv);
            void SetValue([In, MarshalAs(UnmanagedType.Struct)] ref PROPERTYKEY key, [In, MarshalAs(UnmanagedType.Struct)] ref PROPVARIANT pv);
            void Commit();
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
        internal class CShellLink { }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;

            public PROPERTYKEY(Guid guid, uint id)
            {
                fmtid = guid;
                pid = id;
            }

            public static readonly PROPERTYKEY AppUserModel_ID = new PROPERTYKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
            public static readonly PROPERTYKEY AppUserModel_ToastActivatorCLSID = new PROPERTYKEY(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 26);
        }
        #endregion

        internal class PropVariantHelper
        {
            private static class NativeMethods
            {
                [DllImport("Ole32.dll", PreserveSig = false)]
                internal static extern void PropVariantClear(ref PROPVARIANT pvar);
            }

            private PROPVARIANT variant;
            public PROPVARIANT Propvariant
            {
                get { return variant; }
            }

            public VarEnum VarType
            {
                get { return (VarEnum)variant.vt; }
                set { variant.vt = (ushort)value; }
            }

            public void SetValue(Guid value)
            {
                NativeMethods.PropVariantClear(ref variant);
                byte[] guid = ((Guid)value).ToByteArray();
                variant.vt = (ushort)VarEnum.VT_CLSID;
                variant.unionmember = Marshal.AllocCoTaskMem(guid.Length);
                Marshal.Copy(guid, 0, variant.unionmember, guid.Length);
            }

            public void SetValue(string val)
            {
                NativeMethods.PropVariantClear(ref variant);
                variant.vt = (ushort)VarEnum.VT_LPWSTR;
                variant.unionmember = Marshal.StringToCoTaskMemUni(val);
            }
        }
    }
}
