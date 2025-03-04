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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImageViewPage : Page
    {

        private List<FolderItem> ImageList = [];
        public MainPage.GamepadActionDelegate Actions = new();


        public ImageViewPage()
        {
            this.InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var (actions, folder, file) = ((MainPage.GamepadActionDelegate, List<FolderItem>, FolderItem))e.Parameter;

            Actions = actions;

            ImageList = new(folder.Where((item) => item.Type == FolderItem.ItemType.Image));

            FlipView.ItemsSource = ImageList;
            FlipView.SelectedItem = file;
        }

        private void FlipView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Frame.GoBack();
        }

        public void UpAction()
        {
            if (FlipView.SelectedIndex > 0)
                FlipView.SelectedIndex--;
            else
                FlipView.SelectedIndex = ImageList.Count - 1;
        }
        public void DownAction()
        {
            if (FlipView.SelectedIndex < ImageList.Count - 1)
                FlipView.SelectedIndex++;
            else
                FlipView.SelectedIndex = 0;
        }
        public void LeftAction()
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
        public void RightAction()
        {
        }
        public void SelectAction()
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Actions.Up = UpAction;
            Actions.Down = DownAction;
            Actions.Left = LeftAction;
            Actions.Right = RightAction;
            Actions.Select = SelectAction;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
