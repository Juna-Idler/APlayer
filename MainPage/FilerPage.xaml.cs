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

        private SaveData.Folder? SavedFolder = null;
        private SaveData.List? SavedList = null;


        public FilerPage()
        {
            this.InitializeComponent();
            Flyout = (Flyout)Resources["BackToSelector"];
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.Main.ButtonsChanged += Gamepad_ButtonsChanged;
            if (Initialized)
                return;

        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.Main.ButtonsChanged -= Gamepad_ButtonsChanged;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Initialized)
                return;
            (SaveData.List list, SaveData.Folder sfolder, StorageFolder folder, Frame frame) = ((SaveData.List, SaveData.Folder, StorageFolder, Frame))e.Parameter;
            SavedFolder = sfolder;
            SavedList = list;

            Folder = folder;
            WindowFrame = frame;

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

        private void Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased,
            XInput.EventGenerator.AnalogButtons a_pressed, XInput.EventGenerator.AnalogButtons a_released) e)
        {
            CurrentFilerView?.OnGamepadButtonChanged(sender, e);
        }


        private void Fvc_RequestedFile(object? sender, (List<FolderItem> folder, FolderItem file) e)
        {
            switch (e.file.Type)
            {
                case FolderItem.ItemType.Audio:
                    {
                        Frame.Navigate(typeof(PlaylistPage),e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromBottom });
                    }
                    break;
                case FolderItem.ItemType.Image:
                    {
                        Frame.Navigate(typeof(ImageViewPage), e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                    break;
                case FolderItem.ItemType.Text:
                    {
                        Frame.Navigate(typeof(TextViewPage), e,
                            new SlideNavigationTransitionInfo()
                            { Effect = SlideNavigationTransitionEffect.FromRight });
                    }
                    break;
                case FolderItem.ItemType.Pdf:
                    {
                        Frame.Navigate(typeof(PdfViewPage), e,
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
            App.Gamepad.Main.ButtonsChanged -= Gamepad_ButtonsChanged;
            App.Gamepad.Main.ButtonsChanged += OnFlyout_Gamepad_ButtonsChanged; 
        }

        private bool Terminating = false;
        private void Flyout_Closed(object sender, object e)
        {
            if (Terminating)
                Terminating = false;
            else
                App.Gamepad.Main.ButtonsChanged += Gamepad_ButtonsChanged;

            App.Gamepad.Main.ButtonsChanged -= OnFlyout_Gamepad_ButtonsChanged;
        }

        private void BackToStartPage()
        {
            Terminating = true;
            Flyout.Hide();
            WindowFrame?.Navigate(typeof(StartPage.StartPage));
        }

        private void OnFlyout_Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased,
            XInput.EventGenerator.AnalogButtons a_pressed, XInput.EventGenerator.AnalogButtons a_released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    BackToStartPage();
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                {
                    Flyout.Hide();
                }
            });
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
