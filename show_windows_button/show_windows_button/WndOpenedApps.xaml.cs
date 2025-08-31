using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinRT.Interop;

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
            // обработчик потери фокуса
            this.Activated += OnWindowActivated;
            InitializeComponent();
            MoveToCursorPos();
            HideDecoration();




            Refresh();
        }

        private void Refresh()
        {
            Debug.WriteLine("[WndOpenedApps] : Refresh");
            ItemsHost.ItemsSource = NativeWindowEnumerator.GetAllOpenedWindows();
        }

        private void HideDecoration()
        {
            Debug.WriteLine("[WndOpenedApps] : HideDecoration");
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(wndId);

            // полностью убираем заголовок и кнопки
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false); // false == убрать title-bar
                presenter.IsResizable = false;               // (по желанию)
            }

            appWindow.Resize(new Windows.Graphics.SizeInt32(320, 480));
        }

        private void MoveToCursorPos()
        {
           
        }

        private void OnWindowButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[WndOpenedApps] : OnWindowButtonClick");
            if (sender is Button btn && btn.Tag is long hwnd)
                BringToFront((IntPtr)hwnd);
            HideWndOpenedApps();
        }

        private void HideWndOpenedApps()
        {
            Debug.WriteLine("[WndOpenedApps] : HideWndOpenedApps");
            Close();
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            Debug.WriteLine($"[WndOpenedApps] : OnWindowActivated : {args.WindowActivationState}");
            if (args.WindowActivationState == WindowActivationState.Deactivated) Close();
        }

        #region Win32 activate
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private void BringToFront(IntPtr hWnd)
        {
            Debug.WriteLine("[WndOpenedApps] : OnWindowButtonClick");
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }
        #endregion


    }
}
