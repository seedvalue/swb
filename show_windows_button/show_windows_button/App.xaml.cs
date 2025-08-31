//App.xaml.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace show_windows_button
{
   
    public partial class App : Application
    {
      
        public App()
        {
            InitializeComponent();
        }


        WndOpenedApps _openedApps;

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
          
            //TEST LIST CURR WINDOWS
            var openedAll = NativeWindowEnumerator.GetAllOpenedWindows();

            //  _helperWindow = new TaskbarHelperWindow();
            //   _helperWindow.Activated += OnTaskbarIconClicked;
            //_helperWindow.Activate();
            _openedApps = new WndOpenedApps();
            _openedApps.Activate();
            //_openedApps.HideWndOpenedApps();
        }

      

        WndOpenedApps _popupWindow;

        private void OnTaskbarIconClicked(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                if (_popupWindow == null || _popupWindow.IsClosed)
                {
                    _popupWindow = new WndOpenedApps();
                    _popupWindow.Closed += (s, e) => _popupWindow = null;
                    _popupWindow.Activate(); // ← ТОЛЬКО здесь активируем!
                    _popupWindow.MoveToCursorPos();
                }
            }
        }

      



    }
}
