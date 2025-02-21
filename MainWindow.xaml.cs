using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI;
using WinRT.Interop;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {


        public MainWindow()
        {
            this.InitializeComponent();

            SetTitleBar(AppTitleBar);
            ExtendsContentIntoTitleBar = true;

            RightPaddingColumn.Width =
                new GridLength(AppWindow.TitleBar.RightInset / this.Content.RasterizationScale);
            LeftPaddingColumn.Width =
                new GridLength(AppWindow.TitleBar.LeftInset / this.Content.RasterizationScale);
            AppTitleTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
            TitleHeightRow.Height = new GridLength(AppWindow.TitleBar.Height / this.Content.RasterizationScale);

            Activated += MainWindow_Activated;

            MainFrame.Navigate(typeof(FolderSelect));
        }


        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                AppTitleTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                AppTitleTextBlock.Foreground =
                    (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
            }
        }
    }

}
