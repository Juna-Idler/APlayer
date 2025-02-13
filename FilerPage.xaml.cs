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
using Windows.Storage;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Media.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public sealed partial class FilerPage : Page
    {
        private ObservableCollection<Crumb> Crumbs = [];

        private StorageFolder? Folder = null;
        private bool Initialized = false;


        public FilerPage()
        {
            this.InitializeComponent();

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Initialized)
                return;

            Grid.Children.RemoveAt(Grid.Children.Count - 1);
            if (Folder == null)
                throw new InvalidDataException();
            var fvc = new FilerViewControl(Folder, 0);
            fvc.RequestedBack += Fvc_RequestedBack;
            fvc.RequestedFolder += Fvc_RequestedFolder;
            fvc.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(fvc, 1);
            Grid.SetColumnSpan(fvc, 2);

            Grid.Children.Add(fvc);

            var names = Folder.Path.Split(Path.DirectorySeparatorChar);
            Crumbs = [new Crumb(fvc)];
            FolderBreadcrumbBar.ItemsSource = Crumbs;

            Initialized = true;
        }

        private async void Fvc_RequestedFile(object? sender, (List<FolderItem> folder, FolderItem file) e)
        {
            switch (e.file.Type)
            {
                case FolderItem.ItemType.Audio:
                    {
                        List<IStorageFile> items = new(e.folder
                            .Where(item => item.Type == FolderItem.ItemType.Audio)
                            .Select(item => (IStorageFile)item.Item));

                        var index = items.FindIndex(0, item => item.Name == e.file.Name);
                        await App.SoundPlayer.SetPlayList(items, index);
                        App.SoundPlayer.Play();
                    }
                    break;
                case FolderItem.ItemType.Image:
                    {
                        Frame.Navigate(typeof(ImageViewPage), e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromLeft });
                    }
                    break;
                case FolderItem.ItemType.Text:
                    {
                        Frame.Navigate(typeof(TextViewPage), e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromLeft });
                    }
                    break;
                case FolderItem.ItemType.Pdf:
                    {
                        Frame.Navigate(typeof(PdfViewPage), e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromLeft });
                    }
                    break;
            }

        }

        private void Fvc_RequestedBack(object? sender, FilerViewControl e)
        {
            var last = Grid.Children.Last() as FilerViewControl;
            if (last != null)
            {
                last.RequestedBack -= Fvc_RequestedBack;
                last.RequestedFolder -= Fvc_RequestedFolder;
                last.RequestedFile -= Fvc_RequestedFile;
                Grid.Children.Remove(last);
            }
            e.RequestedFolder += Fvc_RequestedFolder;
            e.RequestedBack += Fvc_RequestedBack;
            e.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(e, 1);
            Grid.SetColumnSpan(e, 2);
            Grid.Children.Add(e);

            BackButton.IsEnabled = e.Depth > 0;
            Crumbs.RemoveAt(Crumbs.Count - 1);
        }

        private void Fvc_RequestedFolder(object? sender, FilerViewControl e)
        {
            var last = Grid.Children.Last() as FilerViewControl;
            if (last != null)
            {
                last.RequestedBack -= Fvc_RequestedBack;
                last.RequestedFolder -= Fvc_RequestedFolder;
                last.RequestedFile -= Fvc_RequestedFile;
                Grid.Children.Remove(last);
            }
            e.RequestedFolder += Fvc_RequestedFolder;
            e.RequestedBack += Fvc_RequestedBack;
            e.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(e, 1);
            Grid.SetColumnSpan(e, 2);
            Grid.Children.Add(e);

            BackButton.IsEnabled = true;

            Crumbs.Add(new Crumb(e));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Initialized)
                return;
            var param = e.Parameter as StorageFolder;
            if (param != null)
            {
                Folder = param;
            }
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void FolderBreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (args.Index < Crumbs.Count - 1)
            {
                var last = Grid.Children.Last() as FilerViewControl;
                if (last != null)
                {
                    last.RequestedBack -= Fvc_RequestedBack;
                    last.RequestedFolder -= Fvc_RequestedFolder;
                    last.RequestedFile -= Fvc_RequestedFile;
                    Grid.Children.Remove(last);
                }
                var folder = (args.Item as Crumb)?.Folder;
                if (folder != null)
                {
                    folder.RequestedFolder += Fvc_RequestedFolder;
                    folder.RequestedBack += Fvc_RequestedBack;
                    folder.RequestedFile += Fvc_RequestedFile;
                    Grid.SetRow(folder, 1);
                    Grid.SetColumnSpan(folder, 2);
                    Grid.Children.Add(folder);

                    BackButton.IsEnabled = folder.Depth > 0;
                    while (Crumbs.Count > args.Index + 1)
                    {
                        Crumbs.RemoveAt(Crumbs.Count - 1);
                    }
                }
            }
        }
    }



    public class Crumb(FilerViewControl folder)
    {
        public readonly FilerViewControl Folder = folder;
        public override string ToString() => Folder.Folder.Name;
    }
}
