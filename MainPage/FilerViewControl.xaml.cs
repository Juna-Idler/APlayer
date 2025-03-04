using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class FilerViewControl : UserControl
    {
        public event EventHandler<FilerViewControl?>? RequestedBack;
        public event EventHandler<FilerViewControl>? RequestedFolder;

        public event EventHandler<(List<FolderItem>,FolderItem)>? RequestedFile;



        private List<FolderItem> Items = [];

        public StorageFolder Folder { get; private set; }
        public uint Depth { get; private set; }

        public FilerViewControl? ParentFolder { get; private set; }

        private SaveData.Folder SavedFolder;
        private SaveData.List SavedList;

        public FilerViewControl(SaveData.List saved_list, SaveData.Folder saved_folder,
            StorageFolder folder, uint depth = 0, FilerViewControl? parent = null)
        {
            this.InitializeComponent();
            SavedFolder = saved_folder;
            SavedList = saved_list;

            Folder = folder;
            Depth = depth;
            ParentFolder = parent;

            BuildListView();
        }

        private async void BuildListView()
        {
            var items = await Folder.GetItemsAsync();
            Items = new List<FolderItem>(items.Select(item => new FolderItem(item)).OrderBy(item=>item.Extention));
            foreach (var item in Items)
            {
                _ = item.SetExtra();
            }
            FolderListView.ItemsSource = Items;
            FolderListView.SelectedIndex = 0;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        void EnterFolder(FolderItem item)
        {
            if (item.Created != null)
            {
                RequestedFolder?.Invoke(this, item.Created);
            }
            else
            {
                var folder = item.Item as StorageFolder;
                if (folder != null)
                {
                    var chiled = new FilerViewControl(SavedList,SavedFolder,folder, Depth + 1, this);
                    item.Created = chiled;
                    RequestedFolder?.Invoke(this, chiled);
                }
            }
        }


        public void UpAction()
        {
            if (FolderListView.SelectedIndex > 0)
                FolderListView.SelectedIndex--;
            else
                FolderListView.SelectedIndex = Items.Count - 1;
            FolderListView.ScrollIntoView(FolderListView.SelectedItem);
        }
        public void DownAction()
        {
            if (FolderListView.SelectedIndex < Items.Count - 1)
                FolderListView.SelectedIndex++;
            else
                FolderListView.SelectedIndex = 0;
            FolderListView.ScrollIntoView(FolderListView.SelectedItem);
        }
        public void LeftAction()
        {
            RequestedBack?.Invoke(this, ParentFolder);
        }
        public void RightAction()
        {
            if (FolderListView.SelectedItem != null)
            {
                var item = FolderListView.SelectedItem as FolderItem;
                if (item != null)
                {
                    if (item.Type == FolderItem.ItemType.Folder)
                        EnterFolder(item);
                    else
                        RequestedFile?.Invoke(this, (Items, item));
                }
            }
        }


        private void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid != null)
            {
                var item = grid.DataContext as FolderItem;
                if (item != null)
                {
                    if (item.Type == FolderItem.ItemType.Folder)
                        EnterFolder((FolderItem)item);
                    else
                        RequestedFile?.Invoke(this, (Items, item));
                }
            }

        }

        private void MenuFlyoutItemAddStartPage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem { DataContext: FolderItem folder })
                SavedList.Folders.Add(new SaveData.Folder(folder.Name, folder.Item.Path));
        }
    }
    public partial class FolderItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FolderItem(IStorageItem storageItem)
        {
            Item = storageItem;

            if (Item.IsOfType(StorageItemTypes.Folder))
            {
                Type = ItemType.Folder;
                Extention = "";
            }
            else if (Item.IsOfType(StorageItemTypes.File))
            {
                var file = (StorageFile)Item;
                if (file.ContentType.StartsWith("audio"))
                    Type = ItemType.Audio;
                else if (file.ContentType.StartsWith("image"))
                    Type = ItemType.Image;
                else if (file.ContentType.StartsWith("text"))
                    Type = ItemType.Text;
                else if (file.ContentType == "application/pdf")
                    Type = ItemType.Pdf;
                else
                    Type = ItemType.Unknown;
                Extention = file.FileType.ToUpper();
            }
            else
            {
                Type = ItemType.Unknown;
                Extention = "???";
            }
        }


        public IStorageItem Item { get; }
        public FilerViewControl? Created { get; set; } = null;

        public enum ItemType {Unknown, Folder, Audio, Image, Text, Pdf }

        public ItemType Type { get; }

        public string Extention { get; }

        public string Name { get => Item.Name; }
        public object? Extra { get;private set; } = null;
        public async Task SetExtra()
        {
            switch (Type)
            {
                case ItemType.Folder:
                    break;
                case ItemType.Audio:
                    {
                        var media = MediaSource.CreateFromStorageFile((StorageFile)Item);
                        await media.OpenAsync();
                        if (media.Duration == null)
                            Extra = "XX:XX";
                        else
                        {
                            TimeSpan d = media.Duration.Value;
                            Extra = Math.Floor(d.TotalMinutes).ToString("F0") + ":" +  d.ToString(@"ss");

                        }
                        NotifyPropertyChanged(nameof(Extra));
                    }
                    break;
                case ItemType.Image:
                    {
                    }
                    break;
                case ItemType.Text:
                    break;
                case ItemType.Pdf:
                    break;
                default:
                    break;
            }
        }
    }

    public partial class FolderItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Folder { get; set; } = new();
        public DataTemplate Audio { get; set; } = new();
        public DataTemplate Image { get; set; } = new();
        public DataTemplate Text { get; set; } = new();
        public DataTemplate Pdf { get; set; } = new();
        public DataTemplate Unknown { get; set; } = new();


        protected override DataTemplate SelectTemplateCore(object item)
        {
            var i = (FolderItem)item;
            switch (i.Type)
            {
                case FolderItem.ItemType.Folder:
                    return Folder;
                case FolderItem.ItemType.Audio:
                    return Audio;
                case FolderItem.ItemType.Image:
                    return Image;
                case FolderItem.ItemType.Text:
                    return Text;
                case FolderItem.ItemType.Pdf:
                    return Pdf;
                default:
                    return Unknown;
            }
        }
    }

}
