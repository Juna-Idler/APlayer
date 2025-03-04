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
using APlayer.StartPage;

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

        private FilerViewControl? CurrentFilerView = null;
        private readonly Flyout Flyout;
        private Frame? WindowFrame = null;

        public class NavigationParameter(MainPage.GamepadActionDelegate actions,SaveData.List list,SaveData.Folder sd_folder,StorageFolder folder,Frame frame)
        {
            public MainPage.GamepadActionDelegate Actions = actions;
            public SaveData.List List = list;
            public SaveData.Folder SDFolder = sd_folder;
            public StorageFolder Folder = folder;
            public Frame Frame = frame;
        }

        private MainPage.GamepadActionDelegate Actions = new();
        private SaveData.Folder? SavedFolder = null;
        private SaveData.List? SavedList = null;


        public FilerPage()
        {
            this.InitializeComponent();
            Flyout = (Flyout)Resources["BackToSelector"];
        }

        private void SetActions()
        {
            Actions.Up = () => CurrentFilerView?.UpAction();
            Actions.Down = () => CurrentFilerView?.DownAction();
            Actions.Left = () => CurrentFilerView?.LeftAction();
            Actions.Right = () => CurrentFilerView?.RightAction();
            Actions.Select = () => CurrentFilerView?.RightAction();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetActions();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Initialized)
                return;
            NavigationParameter param = (NavigationParameter)e.Parameter;
            Actions = param.Actions;
            SavedFolder = param.SDFolder;
            SavedList = param.List;
            Folder = param.Folder;
            WindowFrame = param.Frame;

            var fvc = new FilerViewControl(SavedList, SavedFolder, Folder, 0);
            fvc.RequestedBack += Fvc_RequestedBack;
            fvc.RequestedFolder += Fvc_RequestedFolder;
            fvc.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(fvc, 1);
            Grid.SetColumnSpan(fvc, 2);
            Grid.Children.Add(fvc);
            CurrentFilerView = fvc;

            var names = Folder.Path.Split(Path.DirectorySeparatorChar);
            Crumbs = [new Crumb(fvc)];
            FolderBreadcrumbBar.ItemsSource = Crumbs;

            Initialized = true;
        }


        private void Fvc_RequestedFile(object? sender, (List<FolderItem> folder, FolderItem file) e)
        {
            switch (e.file.Type)
            {
                case FolderItem.ItemType.Audio:
                    {
                        Frame.Navigate(typeof(PlaylistPage),(Actions,e.folder,e.file),
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromBottom });
                    }
                    break;
                case FolderItem.ItemType.Image:
                    {
                        Frame.Navigate(typeof(ImageViewPage), (Actions, e.folder, e.file),
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                    break;
                case FolderItem.ItemType.Text:
                    {
                        Frame.Navigate(typeof(TextViewPage), (Actions, e.folder, e.file),
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                    break;
                case FolderItem.ItemType.Pdf:
                    {
                        Frame.Navigate(typeof(PdfViewPage), (Actions, e.folder, e.file),
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                    break;
            }

        }

        private void Fvc_RequestedBack(object? sender, FilerViewControl? e)
        {
            BackAction();
        }

        private void Fvc_RequestedFolder(object? sender, FilerViewControl e)
        {
            if (CurrentFilerView == null)
                return;

            CurrentFilerView.RequestedBack -= Fvc_RequestedBack;
            CurrentFilerView.RequestedFolder -= Fvc_RequestedFolder;
            CurrentFilerView.RequestedFile -= Fvc_RequestedFile;
            Grid.Children.Remove(CurrentFilerView);
            e.RequestedFolder += Fvc_RequestedFolder;
            e.RequestedBack += Fvc_RequestedBack;
            e.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(e, 1);
            Grid.SetColumnSpan(e, 2);
            Grid.Children.Add(e);
            CurrentFilerView = e;

            Crumbs.Add(new Crumb(e));
        }




        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackAction();
        }

        private void BackAction()
        {
            if (CurrentFilerView == null)
                return;
            var parent = CurrentFilerView.ParentFolder;
            if (parent == null)
            {
                Flyout.ShowAt(FolderBreadcrumbBar);
                return;
            }

            CurrentFilerView.RequestedBack -= Fvc_RequestedBack;
            CurrentFilerView.RequestedFolder -= Fvc_RequestedFolder;
            CurrentFilerView.RequestedFile -= Fvc_RequestedFile;
            Grid.Children.Remove(CurrentFilerView);

            parent.RequestedFolder += Fvc_RequestedFolder;
            parent.RequestedBack += Fvc_RequestedBack;
            parent.RequestedFile += Fvc_RequestedFile;
            Grid.SetRow(parent, 1);
            Grid.SetColumnSpan(parent, 2);
            Grid.Children.Add(parent);
            CurrentFilerView = parent;

            Crumbs.RemoveAt(Crumbs.Count - 1);
        }

        private void FolderBreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (CurrentFilerView == null)
                return;
            if (args.Index < Crumbs.Count - 1)
            {
                var folder = (args.Item as Crumb)?.Folder;
                if (folder == null)
                    return;

                CurrentFilerView.RequestedBack -= Fvc_RequestedBack;
                CurrentFilerView.RequestedFolder -= Fvc_RequestedFolder;
                CurrentFilerView.RequestedFile -= Fvc_RequestedFile;
                Grid.Children.Remove(CurrentFilerView);

                folder.RequestedFolder += Fvc_RequestedFolder;
                folder.RequestedBack += Fvc_RequestedBack;
                folder.RequestedFile += Fvc_RequestedFile;
                Grid.SetRow(folder, 1);
                Grid.SetColumnSpan(folder, 2);
                Grid.Children.Add(folder);
                CurrentFilerView = folder;

                while (Crumbs.Count > args.Index + 1)
                {
                    Crumbs.RemoveAt(Crumbs.Count - 1);
                }
            }
        }

        private void Flyout_Opened(object sender, object e)
        {
            Actions.Up = CloseFlyout;
            Actions.Down = CloseFlyout;
            Actions.Left = BackToStartPage;
            Actions.Right = CloseFlyout;
            Actions.Select = BackToStartPage;
        }
        private void CloseFlyout()
        {
            Flyout.Hide();
        }

        private void Flyout_Closed(object sender, object e)
        {
            SetActions();
        }

        private void BackToStartPage()
        {
            Flyout.Hide();
            WindowFrame?.Navigate(typeof(StartPage.StartPage));
        }


        private void ApprovalButton_Click(object sender, RoutedEventArgs e)
        {
            BackToStartPage();
        }

        private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsXButton1Pressed)
            {
                BackAction();
                e.Handled = true;
            }
        }
    }


    public class Crumb(FilerViewControl folder)
    {
        public readonly FilerViewControl Folder = folder;
        public override string ToString() => Folder.Folder.Name;
    }
}
