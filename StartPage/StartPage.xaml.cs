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
using System.Diagnostics;
using System.Reflection;
using APlayer.SaveData;
using Windows.ApplicationModel.DataTransfer;
using static APlayer.StartPage.SavedData.Group;
using APlayer.SoundPlayer;

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

            foreach (var item in App.SavedContents.Indexes)
            {
                if (App.SavedLists.TryGetValue(item.FileName, out var list))
                {
                    TabFolderListControl.TabFolderListItems.Add(
                        new TabFolderListItem(
                            item.Name,
                            new ObservableCollection<SavedFolder>(
                                list.Folders.Select(f => new SavedFolder(f.Name, f.Path))),
                            item.FileName));
                }
            }
            if (TabFolderListControl.TabFolderListItems.Count == 0)
            {
                TabFolderListControl.TabFolderListItems.Add(new("First List", [],"list0.json"));
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

        private void TabFolderListControl_SelectedFolder(object sender, (SaveData.Folder, SaveData.List) e)
        {
            this.Frame.Navigate(typeof(MainPage), e,
                new SlideNavigationTransitionInfo()
                { Effect = SlideNavigationTransitionEffect.FromBottom });

        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SoundApiList.SelectedIndex = App.SoundPlayer switch
            {
                WasapiSharedPlayer => 0,
                WasapiExclusivePlayer => 1,
                _ => -1,
            };
            SoundApiList.SelectionChanged += SoundApiList_SelectionChanged;

            var assign = App.AssignData.StartPage.CreateAssign(GetAction);
            App.Gamepad.SetAssign(assign);
            App.AssignDataChanged += App_AssignDataChanged;
        }

        private void App_AssignDataChanged(object? sender, Type e)
        {
            if (e == typeof(GamepadAssign.StartPageGamepadAction))
            {
                var assign = App.AssignData.StartPage.CreateAssign(GetAction);
                App.Gamepad.SetAssign(assign);
            }
        }


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
        }





        private Action GetAction(GamepadAssign.StartPageGamepadAction act)
        {
            return act switch
            {
                GamepadAssign.StartPageGamepadAction.NextFolder => () => this.DispatcherQueue.TryEnqueue(TabFolderListControl.NextFolder),
                GamepadAssign.StartPageGamepadAction.PrevFolder => () => this.DispatcherQueue.TryEnqueue(TabFolderListControl.PrevFolder),
                GamepadAssign.StartPageGamepadAction.Select => () => this.DispatcherQueue.TryEnqueue(TabFolderListControl.Select),
                GamepadAssign.StartPageGamepadAction.PrevTab => () => this.DispatcherQueue.TryEnqueue(TabFolderListControl.PrevTab),
                GamepadAssign.StartPageGamepadAction.NextTab => () => this.DispatcherQueue.TryEnqueue(TabFolderListControl.NextTab),
                _ => () => { },
            };


        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.Handled = true;
            }
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFolder folder)
                    {
                        TabFolderListControl.AddFolder(folder);
                    }
                }
            }
        }

        private void SoundApiList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.SoundPlayer.Terminalize();
            App.SoundPlayer = SoundApiList.SelectedIndex switch
            {
                0 => new WasapiSharedPlayer(),
                1 => new WasapiExclusivePlayer(),
                _ => new NullPlayer(),
            };

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



}
