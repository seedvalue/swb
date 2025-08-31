using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace show_windows_button
{
    public static class NativeWindowEnumerator
    {
        #region P/Invoke
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        // Virtual-desktop helpers (Win10+)
        [ComImport, Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IVirtualDesktopManager
        {
            int GetWindowDesktopId(IntPtr hWnd, out Guid desktopId);
        }

        [ComImport, Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
        private class VirtualDesktopManager { }

        private static readonly IVirtualDesktopManager _vdm =
            (IVirtualDesktopManager)new VirtualDesktopManager();
        #endregion

        public static IReadOnlyList<WindowInfo> GetAllOpenedWindows()
        {
            var list = new List<WindowInfo>();
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                // Исключаем «инструментальные» и «всплывающие» окна
                int exStyle = (int)GetWindowLongPtr(hWnd, GWL_EXSTYLE);
                bool isToolWindow = (exStyle & WS_EX_TOOLWINDOW) != 0;
                bool isAppWindow = (exStyle & WS_EX_APPWINDOW) != 0;
                if (isToolWindow && !isAppWindow) return true;

                var title = new System.Text.StringBuilder(512);
                GetWindowText(hWnd, title, title.Capacity);

                if (string.IsNullOrWhiteSpace(title.ToString())) return true;

                _vdm.GetWindowDesktopId(hWnd, out Guid desktopGuid);
                int desktopIndex = GetDesktopIndex(desktopGuid);

                list.Add(new WindowInfo
                {
                    Hwnd = hWnd.ToInt64(),
                    Title = title.ToString(),
                    Icon = GetIcon(hWnd),
                    DesktopIndex = desktopIndex
                });
                return true;
            }, IntPtr.Zero);

            return list;
        }

        #region helpers
        private static int GetDesktopIndex(Guid desktopGuid)
        {
            // Если GUID совпадает с текущим рабочим столом – вернём 0.
            // Для остальных можно сохранять словарь desktopGuid -> index,
            // но требуется полный IVirtualDesktopManager (более сложный код).
            return 0; // упрощённая версия
        }

        private static BitmapImage GetIcon(IntPtr hWnd)
        {
            try
            {
                uint pid;
                GetWindowThreadProcessId(hWnd, out pid);
                var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                using var icon = Icon.ExtractAssociatedIcon(proc.MainModule.FileName);
                if (icon == null) return null;

                using var ms = new MemoryStream();
                icon.ToBitmap().Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var bitmap = new BitmapImage();
                // синхронно
                bitmap.SetSource(ms.AsRandomAccessStream());
                return bitmap;
            }
            catch
            {
                return null;
            }
            #endregion
        }
    }

    public sealed class WindowInfo
    {
        public long Hwnd { get; init; }
        public string Title { get; init; } = string.Empty;
        public BitmapImage? Icon { get; init; }
        public int DesktopIndex { get; init; }
    }
}