using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public sealed partial class MainWindow : Window
    {
        private WndOpenedApps _appsWindow;

        public void OnClickShowAll(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnClickShowAll");
            if (_appsWindow == null)
            {
                _appsWindow = new WndOpenedApps();
                _appsWindow.Closed += (s, args) => _appsWindow = null;
            }

            _appsWindow.Activate();   // показать/активировать
        }

        public void OnClickHideAll(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnClickHideALl");
            _appsWindow?.Close();
            //_appsWindow = null;
        }



        public MainWindow()
        {
            InitializeComponent();
        }


    }
}
