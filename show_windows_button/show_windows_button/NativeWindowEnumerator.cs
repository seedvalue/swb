using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace show_windows_button
{
    public static class NativeWindowEnumerator
    {
        #region Win32
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        #endregion

        #region Virtual-desktop COM
        [ComImport, Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVirtualDesktopManager
        {
            int GetWindowDesktopId(IntPtr hWnd, out Guid desktopId);
        }

        [ComImport, Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
        private class VirtualDesktopManager { }

        [ComImport]
        [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
            [PreserveSig]
            int QueryService(
                ref Guid guidService,
                ref Guid riid,
                [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
        }

        [ComImport, Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVirtualDesktopManagerInternal
        {
            int GetCount(IntPtr hWndOrMon);
            void GetDesktops(out IObjectArray desktops);
            void GetDesktop(int index, out IVirtualDesktop desktop);
        }

        [ComImport, Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IObjectArray
        {
            void GetCount(out int count);
            void GetAt(int index, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }

        [ComImport, Guid("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVirtualDesktop
        {
            Guid GetId();
        }
        #endregion

        private static readonly IVirtualDesktopManager _vdm =
            (IVirtualDesktopManager)new VirtualDesktopManager();

        /// <summary>Возвращает все открытые окна всех виртуальных рабочих столов.</summary>
        public static IReadOnlyList<WindowInfo> GetAllOpenedWindows()
        {
            var list = new List<WindowInfo>();

            // карта desktopGuid -> index
            var desktopMap = BuildDesktopMap();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                int exStyle = (int)GetWindowLongPtr(hWnd, GWL_EXSTYLE);
                bool isToolWindow = (exStyle & WS_EX_TOOLWINDOW) != 0;
                bool isAppWindow = (exStyle & WS_EX_APPWINDOW) != 0;
                if (isToolWindow && !isAppWindow) return true;

                var title = new StringBuilder(512);
                GetWindowText(hWnd, title, title.Capacity);

                _vdm.GetWindowDesktopId(hWnd, out Guid desktopGuid);
                desktopMap.TryGetValue(desktopGuid, out int desktopIndex);

                list.Add(new WindowInfo
                {
                    Hwnd = hWnd.ToInt64(),
                    Title = title.Length == 0 ? "<no title>" : title.ToString(),
                    Icon = GetIcon(hWnd),
                    DesktopIndex = desktopIndex
                });
                return true;
            }, IntPtr.Zero);
            
            DebugList(list);
            return list;
        }

        private static void DebugList(List<WindowInfo> ls)
        {
            foreach (WindowInfo info in ls) 
            {
                info.DebugInfo();
            }
        }

        private static Dictionary<Guid, int> BuildDesktopMap()
        {
            var map = new Dictionary<Guid, int>();
            try
            {
                var sp = (IServiceProvider)new VirtualDesktopManager();
                Guid iidIVDM = new Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6");
                sp.QueryService(ref iidIVDM, ref iidIVDM, out object ppv);
                var vdm = (IVirtualDesktopManagerInternal)ppv;

                vdm.GetDesktops(out IObjectArray arr);
                arr.GetCount(out int count);
                for (int i = 0; i < count; i++)
                {
                    arr.GetAt(i, typeof(IVirtualDesktop).GUID, out object deskObj);
                    var desktop = (IVirtualDesktop)deskObj;
                    map[desktop.GetId()] = i;
                }
            }
            catch { /* старые ОС без виртуальных рабочих столов */ }
            return map;
        }

        #region Icon extraction (Win32 → BitmapImage)
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_GETICON = 0x007F;
        private const uint ICON_SMALL2 = 2;

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        private static BitmapImage? GetIcon(IntPtr hWnd)
        {
            IntPtr hIcon = SendMessage(hWnd, WM_GETICON, (IntPtr)ICON_SMALL2, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = SendMessage(hWnd, WM_GETICON, IntPtr.Zero, IntPtr.Zero); // ICON_BIG

            if (hIcon == IntPtr.Zero) // fallback – значок по EXE
            {
                uint pid = 0;
                GetWindowThreadProcessId(hWnd, out pid);
                string exe = System.Diagnostics.Process.GetProcessById((int)pid).MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(exe))
                {
                    var shfi = new SHFILEINFO();
                    IntPtr hres = SHGetFileInfo(exe, 0, ref shfi, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_SMALLICON);
                    hIcon = hres != IntPtr.Zero ? shfi.hIcon : IntPtr.Zero;
                }
            }

            if (hIcon == IntPtr.Zero) return null;

            BitmapImage bmp = new();
            using var ico = System.Drawing.Icon.FromHandle(hIcon);
            using var ms = new MemoryStream();
            ico.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            bmp.SetSource(ms.AsRandomAccessStream());
            DestroyIcon(hIcon);
            return bmp;
        }
        #endregion

    }

    public sealed class WindowInfo
    {
        public long Hwnd { get; init; }
        public string Title { get; init; } = string.Empty;
        public BitmapImage? Icon { get; init; }
        public int DesktopIndex { get; init; }

        public void DebugInfo() => Debug.WriteLine($"[WindowInfo] : DebugInfo : DesktopIndex={DesktopIndex}, Hwnd={Hwnd}, Title={Title}, IconPixelHeight={Icon?.PixelHeight}");
    }
}