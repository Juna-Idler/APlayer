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
using Microsoft.UI.Input;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private string LocalFolder = "";

        public MainWindow()
        {
            LocalFolder = ApplicationData.Current.LocalFolder.Path;

            this.InitializeComponent();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var rect = localSettings.Values["WindowPosSize"] as Rect?;
            if (rect is Rect r)
            {
                RectInt32 ir = new((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
                AppWindow.MoveAndResize(ir);
            }

            var theme = localSettings.Values["Theme"] as int?;
            if (theme is int t)
            {
                ThemeList.SelectedIndex = t;
            }
            else
            {
                ThemeList.SelectedIndex = 0;
            }
            var backdrop = localSettings.Values["Backdrop"] as int?;
            if (backdrop is int b)
            {
                BackdropList.SelectedIndex = b;
            }
            else
            {
                BackdropList.SelectedIndex = 0;
            }
            XInputUser.SelectedIndex = (int)App.Gamepad.UserIndex;

            SetTitleBar(AppTitleBar);
            ExtendsContentIntoTitleBar = true;

            RightPaddingColumn.Width =
                new GridLength(AppWindow.TitleBar.RightInset / this.Content.RasterizationScale);
            LeftPaddingColumn.Width =
                new GridLength(AppWindow.TitleBar.LeftInset / this.Content.RasterizationScale);
            AppTitleTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
            TitleHeightRow.Height = new GridLength(AppWindow.TitleBar.Height / this.Content.RasterizationScale);

            //            Activated += MainWindow_Activated;


            MainFrame.Navigate(typeof(StartPage.StartPage));
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                // Set the initial interactive regions.
                SetRegionsForCustomTitleBar();
            }
        }
        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                // Update interactive regions if the size of the window changes.
                SetRegionsForCustomTitleBar();
            }
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

        private void SetRegionsForCustomTitleBar()
        {
            // Specify the interactive regions of the title bar.

            double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

            RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

            var transform = SettingButton.TransformToVisual(null);
            Rect bounds = transform.TransformBounds(new Rect(0, 0,
                                                             SettingButton.ActualWidth,
                                                             SettingButton.ActualHeight));
            var rect = GetRect(bounds, scaleAdjustment);
            var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);
            nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, [rect]);
        }
        private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        {
            return new Windows.Graphics.RectInt32(
                _X: (int)Math.Round(bounds.X * scale),
                _Y: (int)Math.Round(bounds.Y * scale),
                _Width: (int)Math.Round(bounds.Width * scale),
                _Height: (int)Math.Round(bounds.Height * scale)
            );
        }

        private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((FrameworkElement)Content).RequestedTheme = (ElementTheme)ThemeList.SelectedIndex;
        }

        private void Backdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SystemBackdrop? backdrop = BackdropList.SelectedIndex switch
            {
                0 => new MicaBackdrop(),
                1 => new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                2 => new DesktopAcrylicBackdrop(),
                _ => null,
            };
            if (backdrop != null)
                SystemBackdrop = backdrop;
        }
        private void XInputUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Gamepad.UserIndex = (uint)XInputUser.SelectedIndex;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var user = localSettings.Values["GamepadUserIndex"] as uint?;
            if (user == null || user != App.Gamepad.UserIndex)
                localSettings.Values["GamepadUserIndex"] = App.Gamepad.UserIndex;
            var interval = localSettings.Values["GamepadInterval"] as TimeSpan?;
            if (interval == null || interval != App.Gamepad.Interval)
                localSettings.Values["GamepadInterval"] = App.Gamepad.Interval;

            var theme = localSettings.Values["Theme"] as int?;
            if (theme == null || theme != ThemeList.SelectedIndex)
            {
                localSettings.Values["Theme"] = ThemeList.SelectedIndex;
            }
            var backdrop = localSettings.Values["Backdrop"] as int?;
            if (backdrop == null || backdrop != BackdropList.SelectedIndex)
            {
                localSettings.Values["Backdrop"] = BackdropList.SelectedIndex;
            }

            PointInt32 pos = AppWindow.Position;
            SizeInt32 size = AppWindow.Size;
            Rect rect = new(pos.X,pos.Y,size.Width,size.Height);
            var old_rect = localSettings.Values["WindowPosSize"] as Rect?;
            if (old_rect == null || old_rect != rect)
            {
                localSettings.Values["WindowPosSize"] = rect;
            }

            if (App.SaveFolder != null)
            {
                foreach (var item in App.SavedContents.Indexes)
                {
                    if (App.SavedLists.TryGetValue(item.FileName, out var list))
                    {
                        _ = SaveData.SaveData.SaveList(list, App.SaveFolder, item.FileName);
                    }
                }
                _ = SaveData.SaveData.SaveContents(App.SavedContents, App.SaveFolder);
                _ = SaveData.SaveData.DeleteList(App.SaveFolder, App.DeleteLists);
                App.DeleteLists.Clear();
            }
        }

        private async void GamepadSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new GamepadSettings();
            var dialog = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Gamepad Assign",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                Content = settings,
            };

            dialog.Resources["ContentDialogMaxWidth"] = 1080;

            var primary = App.Gamepad.PrimaryAssign;
            var shifted = App.Gamepad.ShiftedAssign;
            App.Gamepad.ResetAssign();
            if (primary.SourceDataType == typeof(SaveData.GamepadAssign.MainPageGamepadAction))
            {
                settings.SetTabIndex(1);
            }

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                settings.Save(primary.SourceDataType);

                if (App.SaveFolder != null)
                {
                    await SaveData.GamepadAssign.Save(App.AssignData, App.SaveFolder);
                }
            }
            else
            {
                App.Gamepad.SetAssign(primary,shifted);
            }
        }
    }

}
