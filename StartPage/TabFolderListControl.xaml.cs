using APlayer.SaveData;
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
using System.Collections.Specialized;
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

        public event EventHandler<(SaveData.Folder,SaveData.List)>? SelectedFolder;

        public ObservableCollection<TabFolderListItem> TabFolderListItems { get; private set; } = [];
        public List<string> DeleteItems { get; private set; } = [];

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
                App.SavedContents.Index = selectedIndex;
            }
        }

        private Flyout FolderFlyout { get; set; }
        private Flyout TabHeaderFlyout { get; set; }
        private Flyout AddTabFlyout { get; set; }


        public TabFolderListControl()
        {
            this.InitializeComponent();
            TabFolderListItems.CollectionChanged += (s, e) => UpdateContents();

            FolderFlyout = (Flyout)Resources["FolderFlyout"];
            TabHeaderFlyout = (Flyout)Resources["TabItemHeaderFlyout"];
            AddTabFlyout = (Flyout)Resources["AddTabFlyout"];
        }

        public void AddFolder(StorageFolder folder)
        {
            if (SelectedIndex < 0)
                return;
            var current = TabFolderListItems[SelectedIndex];
            current.Folders.Add(new SavedFolder(folder.Name, folder.Path));
        }

        private void UpdateContents()
        {
            List<ListIndex> new_indexes = [];
            int i = 0;
            foreach (var item in TabFolderListItems)
            {
                new_indexes.Add(new SaveData.ListIndex(item.Name, item.FileName, i++));
            }
            App.SavedContents.Indexes = new_indexes;
        }


        private void SelectedFolderInvoke(SavedFolder sf)
        {
            SaveData.List list = App.SavedLists[TabFolderListItems[SelectedIndex].FileName];
            var folder = list.Folders.Find(f => f.Path == sf.Path);
            if (folder != null)
            {
                SelectedFolder?.Invoke(this, (folder, list));
            }
        }


        private void Folder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is SavedFolder sf)
                {
                    SelectedFolderInvoke(sf);
                }
            }
        }

        private void FoldersView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is SavedFolder add)
            {
                add.Selected = true;
                if (sender is ListView listView)
                {
                    int index = listView.Items.IndexOf(add);
                    if (index == listView.Items.Count - 1)
                        listView.ScrollIntoView(add,ScrollIntoViewAlignment.Leading);
                    else
                        listView.ScrollIntoView(add);
                }
            }
            if (e.RemovedItems.FirstOrDefault() is SavedFolder rem)
            {
                rem.Selected = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (TabFolderListItems.Count > 0)
            {
                SelectedIndex = Math.Clamp(App.SavedContents.Index, 0, TabFolderListItems.Count - 1);

                var list = TabFolderListItems[SelectedIndex];
                if (list.Folders.Count > 0)
                {
                    int index = Math.Clamp(App.SavedContents.FolderIndex, 0, list.Folders.Count - 1);
//                    list.SelectedIndex = index;
                    if (index > 0)
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            list.SelectedIndex = index;
                        });
                    }
                }
            }
            App.Gamepad.Main.ButtonsChanged += Gamepad_ButtonsChanged;
        }


        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.Main.ButtonsChanged -= Gamepad_ButtonsChanged;
        }


        private void Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased,
            XInput.EventGenerator.AnalogButtons a_pressed, XInput.EventGenerator.AnalogButtons a_released) e)
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
                            SelectedFolderInvoke(folder);
                        }
                    }
                }
            });
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is TabFolderListItem item)
                {
                    if (item.Name != TabNameText.Text)
                    {
                        item.Name = TabNameText.Text;
                        UpdateContents();
                    }
                }
            }
            TabHeaderFlyout.Hide();
        }
        private void TabNameText_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender is FrameworkElement fe)
                {
                    if (fe.DataContext is TabFolderListItem item)
                    {
                        if (item.Name != TabNameText.Text)
                        {
                            item.Name = TabNameText.Text;
                            UpdateContents();
                        }
                    }
                }
                TabHeaderFlyout.Hide();
            }

        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is TabFolderListItem item)
                {
                    DeleteItems.Add(item.FileName);
                    TabFolderListItems.Remove(item);
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
                    TabNameText.Text = item.Name;
                }
            }
        }

        private void TabView_AddTabButtonClick(TabView sender, object args)
        {
            List<string> existing = TabFolderListItems.Select(item => item.FileName).ToList();
            int i = 0;
            string unique_name = string.Format("list{0}.json", i);
            while (existing.Contains(unique_name))
            {
                unique_name = string.Format("list{0}.json", ++i);
            }
            var tab = new TabFolderListItem("new_list", [],unique_name);
            TabFolderListItems.Add(tab);
            tab.UpdateList();
            SelectedIndex  = TabFolderListItems.Count - 1;

            AddTabFlyout.ShowAt(sender);
        }

        private void AddTabFlyout_Opened(object sender, object e)
        {
            AddTabNameText.Text = TabFolderListItems.Last().Name;
        }
        private void AddTabFlyout_Closed(object sender, object e)
        {
            var item = TabFolderListItems.Last();
            item.Name = AddTabNameText.Text;
            UpdateContents();
        }

        private void FolderFlyout_Opened(object sender, object e)
        {
            if (sender is Flyout f)
            {
                if (f.Target.DataContext is SavedFolder folder)
                {
                    FolderNameText.Text = folder.Name;
                }
            }
        }
        private void FolderRenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is SavedFolder folder)
                {
                    if (folder.Name != FolderNameText.Text)
                    {
                        folder.Name = FolderNameText.Text;
                        TabFolderListItems[SelectedIndex].UpdateList();
                    }
                }
            }
            FolderFlyout.Hide();
        }
        private void FolderTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender is FrameworkElement fe)
                {
                    if (fe.DataContext is SavedFolder folder)
                    {
                        if (folder.Name != FolderNameText.Text)
                        {
                            folder.Name = FolderNameText.Text;
                            TabFolderListItems[SelectedIndex].UpdateList();
                        }
                    }
                }
                FolderFlyout.Hide();
            }
        }

        private void FolderDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.DataContext is SavedFolder folder)
                {
                    TabFolderListItems[SelectedIndex].Folders.Remove(folder);
                }
            }
            FolderFlyout.Hide();
        }



        private SavedFolder? MoveItem = null;
        private TabFolderListItem? MoveFrom = null;
        private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            MoveFrom = (sender as FrameworkElement)?.DataContext as TabFolderListItem;
            if (MoveFrom != null)
            {
                e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                MoveItem = e.Items.First() as SavedFolder;
            }
        }

        private void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            if (MoveItem != null)
            {
                if (sender is FrameworkElement { DataContext: TabFolderListItem target })
                {
                    var index =TabFolderListItems.IndexOf(target);
                    TabView.SelectedIndex = index;
                    if (target != MoveFrom)
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                    else
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                }
            }
        }

        private void TabViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (MoveItem != null)
            {
                e.Handled = true;
            }
        }
        private void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            if (MoveItem != null && MoveFrom != null)
            {
                e.Handled = true;
                if ((sender as FrameworkElement)?.DataContext is not TabFolderListItem target)
                    return;
                if (target == MoveFrom)
                    return;
                MoveFrom.Folders.Remove(MoveItem);
                target.Folders.Add(MoveItem);
            }
        }

        private void ListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            MoveItem = null;
            MoveFrom = null;
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (MoveItem != null)
            {
                var target = (sender as FrameworkElement)?.DataContext as TabFolderListItem;
                if (MoveFrom != target)
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                    e.Handled = true;
                }
            }
        }
        private void ListView_DragOver(object sender, DragEventArgs e)
        {
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
        }


    }

    public partial class TabFolderListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabFolderListItem(string name, ObservableCollection<SavedFolder> folders,string file_name)
        {
            this.name = name;
            Folders = folders;
            FileName = file_name;
            selectedIndex = (folders.Count == 0) ? -1 : 0;

            Folders.CollectionChanged += (s, e) => UpdateList();
        }
        public void UpdateList()
        {
            App.SavedLists[FileName] = new List(Name, Folders.Select(f => new Folder(f.Name, f.Path)).ToList());
        }


        public string name;
        public string Name
        {
            get => name;
            set
            {
                if (value == name)
                    return;
                name = value;
                NotifyPropertyChanged();
                UpdateList();
            }
        }
        public ObservableCollection<SavedFolder> Folders { get; set; }

        public string FileName { get; private set; }

        private int selectedIndex;


        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value == selectedIndex)
                    return;
                selectedIndex = value;
                NotifyPropertyChanged();
                App.SavedContents.FolderIndex = selectedIndex;
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
                NotifyPropertyChanged(nameof(FontSize));

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
