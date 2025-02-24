using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer.StartPage
{
    public sealed partial class TabFolderListControl : UserControl , INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler<(string name, string path)>? SelectedFolder;

        public ObservableCollection<TabFolderListItem> TabFolderListItems { get; private set; } = [];
        public bool Updated { get; set; } = false;

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value == selectedIndex)
                    return;
                selectedIndex = value;
                NotifyPropertyChanged();
            }
        }

        private Flyout TabHeaderFlyout { get; set; }
        private Flyout AddTabFlyout { get; set; }


        public TabFolderListControl()
        {
            this.InitializeComponent();
            TabFolderListItems.CollectionChanged += (s, e) => Updated = true;

            TabHeaderFlyout = (Flyout)Resources["TabItemHeaderFlyout"];
            AddTabFlyout = (Flyout)Resources["AddTabFlyout"];
        }

        public void AddFolder(StorageFolder folder)
        {
            if (SelectedIndex < 0)
                return;
            var current = TabFolderListItems[SelectedIndex];
            current.Folders.Add(new SavedFolder(folder.Name, folder.Path));
            Updated = true;
        }

        private void Folder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is SavedFolder sf)
                {
                    SelectedFolder?.Invoke(this,(sf.Name, sf.Path));
                }
            }
        }

        private void FoldersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is SavedFolder add)
            {
                add.Selected = true;
            }
            if (e.RemovedItems.FirstOrDefault() is SavedFolder rem)
            {
                rem.Selected = false;
            }
        }

        private void MenuFlyoutItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if ( sender is MenuFlyoutItem item )
            {
                if (item.DataContext is SavedFolder folder)
                {
                    TabFolderListItems[TabView.SelectedIndex].Folders.Remove(folder);
                    Updated = true;
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (TabFolderListItems.Count > 0)
                SelectedIndex = 0;
            App.Gamepad.ButtonsChanged += Gamepad_ButtonsChanged;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.ButtonsChanged -= Gamepad_ButtonsChanged;
        }


        private void Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons released) e)
        {
            if (SelectedIndex < 0)
                return;
            var current = TabFolderListItems[SelectedIndex];
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    if (current.SelectedIndex > 0)
                        current.SelectedIndex--;
                    else if (current.SelectedIndex == 0)
                        current.SelectedIndex = current.Folders.Count - 1;
                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    if (current.SelectedIndex < current.Folders.Count - 1)
                        current.SelectedIndex++;
                    else if (current.SelectedIndex > 0)
                        current.SelectedIndex = 0;
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    if (SelectedIndex > 0)
                        SelectedIndex--;
                    else if (SelectedIndex == 0)
                        SelectedIndex = TabFolderListItems.Count - 1;
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                {
                    if (SelectedIndex < TabFolderListItems.Count - 1)
                        SelectedIndex++;
                    else if (SelectedIndex > 0)
                        SelectedIndex = 0;
                }

                if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT) ||
                    e.pressed.HasFlag(XInput.Buttons.A))
                {
                    if (current.SelectedIndex >= 0)
                    {
                        if (current.Folders[current.SelectedIndex] is SavedFolder folder)
                        {
                            SelectedFolder?.Invoke(this, (folder.Name, folder.Path));
                        }
                    }
                }
            });
        }
        private void Gamepad_LeftStickButtonsChanged(object? sender, (XInput.EventGenerator.StickButtons pressed, XInput.EventGenerator.StickButtons released) e)
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag( XInput.EventGenerator.StickButtons.Left))
                {
                    
                    if (SelectedIndex > 0)
                        SelectedIndex--;
                }
                if (e.pressed.HasFlag(XInput.EventGenerator.StickButtons.Right))
                {
                    if (SelectedIndex < TabFolderListItems.Count - 1)
                        SelectedIndex++;
                }
            });
        }


        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is TabFolderListItem item)
                {
                    int index = TabFolderListItems.IndexOf(item);
                    if (TabFolderListItems[index].Name != TabNameText.Text)
                    {
                        TabFolderListItems[index].Name = TabNameText.Text;
                        Updated = true;
                    }
                }
            }
            TabHeaderFlyout.Hide();
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is TabFolderListItem item)
                {
                    TabFolderListItems.Remove(item);
                    Updated = true;
                }
            }
            TabHeaderFlyout.Hide();

        }

        private void TabHeaderFlyout_Opened(object sender, object e)
        {
            if (sender is Flyout f)
            {
                if (f.Target.DataContext is TabFolderListItem item)
                {
                    int index = TabFolderListItems.IndexOf(item);
                    TabNameText.Text = TabFolderListItems[index].Name;
                }
            }
        }

        private void TabView_AddTabButtonClick(TabView sender, object args)
        {
            TabFolderListItems.Add(new TabFolderListItem("new_list", []));
            Updated = true;
            SelectedIndex  = TabFolderListItems.Count - 1;
            AddTabFlyout.ShowAt(sender);
        }

        private void AddTabNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            TabFolderListItems.Last().Name = AddTabNameText.Text;
            Updated = true;
        }

        private void AddTabFlyout_Opened(object sender, object e)
        {
            AddTabNameText.Text = TabFolderListItems.Last().Name;
        }
    }

    public partial class TabFolderListItem(string name, ObservableCollection<SavedFolder> folders) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string name = name;
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
        public ObservableCollection<SavedFolder> Folders { get; set; } = folders;

        private int selectedIndex = (folders.Count == 0) ? -1 : 0;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value == selectedIndex)
                    return;
                selectedIndex = value;
                NotifyPropertyChanged();
            }
        }

    }

    public partial class SavedFolder(string name, string path) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool selected = false;
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
        private string name = name;
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

        public string Path { get; set; } = path;
    }


}
