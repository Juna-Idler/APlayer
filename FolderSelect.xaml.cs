using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Animation;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    [JsonSerializable(typeof(ObservableCollection<SavedFolder>))]
    internal partial class SavedFolderJsonSourceGenerationContext : JsonSerializerContext { }

    public sealed partial class FolderSelect : Page
    {

        public ObservableCollection<SavedFolder> SavedFolders = [];

        public FolderSelect()
        {
            this.InitializeComponent();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            object folders = localSettings.Values["SavedFolders"];
            if (folders is not null and string)
            {
                var f = JsonSerializer.Deserialize<ObservableCollection<SavedFolder>>((string)folders,SavedFolderJsonSourceGenerationContext.Default.ObservableCollectionSavedFolder);
                if (f != null)
                {
                    SavedFolders = f;
                }
            }

            FoldersView.ItemsSource = SavedFolders;
            SavedFolders.CollectionChanged += SavedFolders_CollectionChanged;

            FoldersView.SelectedIndex = 0;

        }

        private void SavedFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.Gamepad.ButtonsChanged -= OnGamepadButtonChanged;
            base.OnNavigatedFrom(e);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.Gamepad.ButtonsChanged += OnGamepadButtonChanged;
            base.OnNavigatedTo(e);
        }



        void OnGamepadButtonChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased) e)
        {
            if (e.pressed.HasFlag(XInput.Buttons.UP))
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (FoldersView.SelectedIndex > 0)
                        FoldersView.SelectedIndex--;
                }
                );
            }
            if (e.pressed.HasFlag(XInput.Buttons.DOWN))
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (FoldersView.SelectedIndex < SavedFolders.Count - 1)
                        FoldersView.SelectedIndex++;
                });
            }
            if (e.pressed.HasFlag(XInput.Buttons.RIGHT) ||
                e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT) ||
                e.pressed.HasFlag(XInput.Buttons.A))
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (FoldersView.SelectedItem != null)
                    {
                        if (FoldersView.SelectedItem is SavedFolder folder)
                        {
                            this.Frame.Navigate(typeof(MainPage), FoldersView.SelectedItem,
                            new DrillInNavigationTransitionInfo());
                        }
                    }
                });
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
                SavedFolders.Add(new SavedFolder() { Name = folder.Name, Path = folder.Path });
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                var json = JsonSerializer.Serialize(SavedFolders, SavedFolderJsonSourceGenerationContext.Default.ObservableCollectionSavedFolder);
                localSettings.Values["SavedFolders"] = json;

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

        private void Folder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (FoldersView.SelectedItem != null)
            {
                this.Frame.Navigate(typeof(MainPage), FoldersView.SelectedItem,
                    new SlideNavigationTransitionInfo()
                    { Effect = SlideNavigationTransitionEffect.FromBottom });
            }
        }
        

        private void FoldersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var add = e.AddedItems.FirstOrDefault() as SavedFolder;
            if (add != null)
            {
                add.Selected = true;
            }
            var rem = e.RemovedItems.FirstOrDefault() as SavedFolder;
            if (rem != null)
            {
                rem.Selected = false;
            }
        }

        private void MenuFlyoutItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuFlyoutItem;
            if (item != null)
            {
                var folder = item.DataContext as SavedFolder;
                if (folder != null)
                {
                    SavedFolders.Remove(folder);
                }
            }
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

            object theme = localSettings.Values["Theme"];
            if (theme is not null and int t)
            {
                Theme.SelectedIndex = t;
            }
            Theme.SelectionChanged += Theme_SelectionChanged;

            object backdrop = localSettings.Values["Backdrop"];
            if (backdrop is not null and int b)
            {
                Backdrop.SelectedIndex = b;
            }
            Backdrop.SelectionChanged += Backdrop_SelectionChanged;

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
                else
                {
                    ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["OutputDevice"] = od.ToString();
                }
            }
        }

        private async void DeviceUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            OutputDeviceList.SelectionChanged -= OutputDeviceList_SelectionChanged;
            var selected = OutputDeviceList.SelectedItem;
            var devices = await App.SoundPlayer.GetDevices();
            List<OutputDevice> list = [new OutputDevice(null)];
            list.AddRange(devices.Select(x => new OutputDevice(x)));
            OutputDeviceList.ItemsSource = list;

            int index = list.FindIndex(x => x.ToString() == selected.ToString());
            if (index == -1)
            {
                var result = await App.SoundPlayer.Initialize();
                OutputDeviceList.SelectedIndex = 0;
            }
            else
                OutputDeviceList.SelectedIndex = index;
            OutputDeviceList.ItemsSource = list;
            OutputDeviceList.SelectionChanged += OutputDeviceList_SelectionChanged;
        }

        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.MainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = (ElementTheme)Theme.SelectedIndex;
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["Theme"] = Theme.SelectedIndex;
            }
        }

        private void Backdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.MainWindow == null)
                return;
            App.MainWindow.SystemBackdrop = Backdrop.SelectedIndex switch
            {
                0 => new MicaBackdrop(),
                1 => new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt },
                2 => new DesktopAcrylicBackdrop(),
                _ => new MicaBackdrop(),
            };
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["Backdrop"] = Backdrop.SelectedIndex;
        }

    }


    public partial class SavedFolder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool selected = false;
        private string name = "";

        [JsonIgnore]
        public bool Selected
        {
            get => selected;
            set
            {
                if (value == selected)
                    return;
                selected = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("FontSize");

            }
        }
        public string Name
        {
            get => name;
            set
            {
                if (value == name)
                    return;
                name = value;
                NotifyPropertyChanged();
            }
        }

        [JsonIgnore]
        public int FontSize
        {
            get
            {
                if (selected)
                    return 32;
                else
                    return 16;
            }
        }

        public string Path { get; set; } = "";

    }




    public class OutputDevice(ISoundPlayer.IDevice? device)
    {
        public ISoundPlayer.IDevice? Device { get; set; } = device;

        public override string ToString()
        {
            if (Device == null)
                return "System Default";
            return Device.Name;
        }
    }
}
