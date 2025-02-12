using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using static APlayer.FilerPage;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Storage;
using Windows.Media.Core;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class FilerViewControl : UserControl
    {
        public event EventHandler<FilerViewControl>? RequestedBack;
        public event EventHandler<FilerViewControl>? RequestedFolder;

        public event EventHandler<(List<FolderItem>,FolderItem)>? RequestedFile;



        private List<FolderItem> Items = [];

        public StorageFolder Folder { get; private set; }
        public uint Depth { get; private set; }

        private readonly FilerViewControl? ParentFolder = null;


        public FilerViewControl(StorageFolder folder,uint depth = 0,FilerViewControl? parent = null)
        {
            this.InitializeComponent();

            Folder = folder;
            Depth = depth;
            ParentFolder = parent;

            BuildListView();
        }

        private async void BuildListView()
        {
            var items = await Folder.GetItemsAsync();
            Items = new List<FolderItem>(items.Select(item => new FolderItem(item)));
            foreach (var item in Items)
            {
                _ = item.SetExtra();
            }
            FolderListView.ItemsSource = Items;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.ButtonsChanged += OnGamepadButtonChanged;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.ButtonsChanged -= OnGamepadButtonChanged;
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
                    var chiled = new FilerViewControl(folder, Depth + 1, this);
                    item.Created = chiled;
                    RequestedFolder?.Invoke(this, chiled);
                }
            }
        }


        void OnGamepadButtonChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    if (FolderListView.SelectedIndex > 0)
                        FolderListView.SelectedIndex--;
                    FolderListView.ScrollIntoView(FolderListView.SelectedItem);
                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    if (FolderListView.SelectedIndex < Items.Count - 1)
                        FolderListView.SelectedIndex++;
                    FolderListView.ScrollIntoView(FolderListView.SelectedItem);
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    if (ParentFolder != null)
                        RequestedBack?.Invoke(this, ParentFolder);
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT) || e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                {
                    if (FolderListView.SelectedItem != null)
                    {
                        var item = FolderListView.SelectedItem as FolderItem;
                        if (item != null)
                        {
                            if (item.Type == FolderItem.ItemType.Folder)
                                EnterFolder((FolderItem)FolderListView.SelectedItem);
                            else
                                RequestedFile?.Invoke(this, (Items,item));
                        }
                    }
                }
            });
        }
    }
    public partial class FolderItem(IStorageItem storageItem) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public IStorageItem Item { get; } = storageItem;
        public FilerViewControl? Created { get; set; } = null;

        public enum ItemType {NULL, Unknown, Folder, Audio, Image, Text, Pdf }

        private ItemType _itemType = ItemType.NULL;
        public ItemType Type { get
            {
                if (_itemType != ItemType.NULL)
                    return _itemType;
                if (Item.IsOfType(StorageItemTypes.Folder))
                    _itemType = ItemType.Folder;
                else if (Item.IsOfType(StorageItemTypes.File))
                {
                    var file = (StorageFile)Item;
                    if (file.ContentType.StartsWith("audio"))
                        _itemType = ItemType.Audio;
                    else if (file.ContentType.StartsWith("image"))
                        _itemType = ItemType.Image;
                    else if (file.ContentType.StartsWith("text"))
                        _itemType = ItemType.Text;
                    else if (file.ContentType == "application/pdf")
                        _itemType = ItemType.Pdf;
                    else
                        _itemType = ItemType.Unknown;
                }
                else
                    _itemType = ItemType.Unknown;

                return _itemType;
            } }

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
                            Extra = "XX:XX:XX";
                        else
                            Extra = media.Duration.Value.ToString(@"hh\:mm\:ss");
                        NotifyPropertyChanged(nameof(Extra));
                    }
                    break;
                case ItemType.Image:
                    {
                    }
                    break;
                case ItemType.Text:
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
                default:
                    return Unknown;
            }
        }
    }

}
