using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Animation;
using APlayer;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer.StartPage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class StartPage : Page
    {

        public StartPage()
        {
            this.InitializeComponent();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            object json = localSettings.Values["SavedData"];
            if (json is not null and string j)
            {
                var saved_data = JsonSerializer.Deserialize<SavedData>(j, SourceGenerationContext.Default.SavedData);
                if (saved_data != null)
                {
                    var items = saved_data.Groups.Select(
                        g => new TabFolderListItem(g.Name, new ObservableCollection<SavedFolder>(g.Folders.Select(
                            f => new SavedFolder(f.Name, f.Path)))));

                    foreach (var item in items)
                    {
                        TabFolderListControl.TabFolderListItems.Add(item);
                    }
                }
            }

            var theme = localSettings.Values["Theme"];
            if (theme is not null and int t)
            {
                ThemeList.SelectedIndex = t;
            }
            var backdrop = localSettings.Values["Backdrop"];
            if (backdrop is not null and int b)
            {
                BackdropList.SelectedIndex = b;
            }
        }


        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            if (senderButton != null)
            {
                senderButton.IsEnabled = false;
            }

            if (App.MainWindow == null)
                throw new InvalidDataException();

            var folder = await OpenFolderPicker(App.MainWindow);

            if (folder != null)
            {
                TabFolderListControl.AddFolder(folder);
            }
            if (senderButton != null)
            {
                senderButton.IsEnabled = true;
            }
        }

        private static async Task<StorageFolder?> OpenFolderPicker(Window window)
        {
            FolderPicker picker = new Windows.Storage.Pickers.FolderPicker();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            // Set options for your folder picker
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                return folder;
            }
            return null;
        }

        private void TabFolderListControl_SelectedFolder(object sender, (string name, string path) e)
        {
            this.Frame.Navigate(typeof(MainPage), e,
                new SlideNavigationTransitionInfo()
                { Effect = SlideNavigationTransitionEffect.FromBottom });

        }

        private async void OutputDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OutputDeviceList.SelectedItem is OutputDevice od)
            {
                if (App.SoundPlayer.OutputDevice == od.Device)
                    return;
                var result = await App.SoundPlayer.Initialize(od.Device);
                if (!result)
                {
                    //Ž¸”s‚µ‚½
                }
            }
        }

        private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var root = App.MainWindow?.Content as FrameworkElement;
            if (root != null)
            {
                root.RequestedTheme = (ElementTheme)ThemeList.SelectedIndex;
            }
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
            if (App.MainWindow != null && backdrop != null)
                App.MainWindow.SystemBackdrop = backdrop;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var devices = await App.SoundPlayer.GetDevices();
            List<OutputDevice> list = [new OutputDevice(null)];
            list.AddRange(devices.Select(x => new OutputDevice(x)));
            OutputDeviceList.ItemsSource = list;
            OutputDeviceList.SelectedIndex = 0;

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            object device_name = localSettings.Values["OutputDevice"];
            if (device_name is not null and string dn)
            {
                int index = list.FindIndex(x => x.ToString() == dn);
                if (index >= 0)
                {
                    var result = await App.SoundPlayer.Initialize(list[index].Device);
                    if (result)
                    {
                        OutputDeviceList.SelectedIndex = index;
                    }
                }
            }
            OutputDeviceList.SelectionChanged += OutputDeviceList_SelectionChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (OutputDeviceList.SelectedItem is OutputDevice od)
            {
                if ((string)localSettings.Values["OutputDevice"] != od.ToString())
                    localSettings.Values["OutputDevice"] = od.ToString();
            }

            if ((int)localSettings.Values["Theme"] != ThemeList.SelectedIndex)
                localSettings.Values["Theme"] = ThemeList.SelectedIndex;
            if ((int)localSettings.Values["Backdrop"] != BackdropList.SelectedIndex)
                localSettings.Values["Backdrop"] = BackdropList.SelectedIndex;

            if (TabFolderListControl.Updated)
            {
                var save_data = new SavedData(TabFolderListControl.TabFolderListItems.Select(
                    item => new SavedData.Group(item.Name, item.Folders.Select(
                        folder => new SavedData.Group.Folder(folder.Name, folder.Path)))));
                string json = JsonSerializer.Serialize(save_data, SourceGenerationContext.Default.SavedData);
                if ((string)localSettings.Values["SavedData"] != json)
                    localSettings.Values["SavedData"] = json;
            }
        }
    }


    [JsonSerializable(typeof(SavedData))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    public class SavedData(IEnumerable<SavedData.Group> groups)
    {
        public IEnumerable<Group> Groups { get; set; } = groups;

        public class Group(string name, IEnumerable<Group.Folder> folders)
        {
            public string Name { get; set; } = name;
            public IEnumerable<Folder> Folders { get; set; } = folders;

            public class Folder(string name, string path)
            {
                public string Name { get; set; } = name;
                public string Path { get; set; } = path;
            }

        }
    }

    public class OutputDevice(ISoundPlayer.IDevice? device)
    {
        public ISoundPlayer.IDevice? Device { get; set; } = device;

        public override string ToString()
        {
            if (Device == null)
                return "Default Device";
            return Device.Name;
        }
    }


}
