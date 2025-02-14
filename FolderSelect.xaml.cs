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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.VoiceCommands;
using System.Text.Json.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FolderSelect : Page
    {

        public ObservableCollection<SavedFolder> savedFolders = [];

        private readonly JsonSerializerOptions options = new()
        {
            IgnoreReadOnlyProperties = true,
            WriteIndented = true
        };

        public FolderSelect()
        {
            this.InitializeComponent();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            object folders = localSettings.Values["SavedFolders"];
            if (folders != null && folders is string)
            {
                var f = JsonSerializer.Deserialize<ObservableCollection<SavedFolder>>((string)folders);
                if (f != null)
                {
                    savedFolders = f;
                }
            }

            FoldersView.ItemsSource = savedFolders;

            FoldersView.SelectedIndex = 0;
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
                    if (FoldersView.SelectedIndex < savedFolders.Count - 1)
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
            //disable the button to avoid double-clicking
            var senderButton = sender as Button;
            if (senderButton != null)
            {
                senderButton.IsEnabled = false;
            }

            // Clear previous returned file name, if it exists, between iterations of this scenario
            PickFolderOutputTextBlock.Text = "";

            if (App.MainWindow == null)
                throw new InvalidDataException();

            var folder = await OpenFolderPicker(App.MainWindow);

            if (folder != null)
            {
                savedFolders.Add(new SavedFolder() { Name = folder.Name, Path = folder.Path });
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                var json = JsonSerializer.Serialize(savedFolders, options);
                localSettings.Values["SavedFolders"] = json;

            }
            //re-enable the button
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
                    savedFolders.Remove(folder);
                }
            }
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



}
