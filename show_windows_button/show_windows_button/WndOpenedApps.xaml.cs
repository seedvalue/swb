using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace show_windows_button
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WndOpenedApps : Window
    {
        public WndOpenedApps()
        {
            InitializeComponent();
            Refresh();
        }

        private void Refresh()
        {
            ItemsHost.ItemsSource = NativeWindowEnumerator.GetAllOpenedWindows();
        }

        private void OnWindowButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long hwnd)
                BringToFront((IntPtr)hwnd);
        }


        #region Win32 activate
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private void BringToFront(IntPtr hWnd)
        {
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }
        #endregion
    }
}
