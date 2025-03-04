using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextViewPage : Page
    {
        public MainPage.GamepadActionDelegate Actions = new();

        private IStorageFile? File = null;

        private readonly DispatcherTimer timer = new();
//        private double repeat_scroll = 0;
        private ScrollingScrollOptions options = new( ScrollingAnimationMode.Auto );

//        const int repeat_scroll_amount = 240;

        public TextViewPage()
        {
            this.InitializeComponent();
//            timer.Interval = TimeSpan.FromSeconds(0.20);
//            timer.Tick += Timer_Tick;
        }

//        private void Timer_Tick(object? sender, object e)
//        {
//            ScrollView.ScrollBy(0, repeat_scroll,options);
//            repeat_scroll *= 1.1;
//        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (actions, folder, file) = ((MainPage.GamepadActionDelegate, List<FolderItem>, FolderItem))e.Parameter;
            Actions = actions;
            File = file.Item as IStorageFile;
        }


        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Actions.Up = UpAction;
            Actions.Down = DownAction;
            Actions.Left = LeftAction;
            Actions.Right = RightAction;
            Actions.Select = SelectAction;

            if (File != null)
            {
                Title.Text = File.Name;
                try
                {
                    TextView.Text = await FileIO.ReadTextAsync(File);
                }
                catch (Exception)
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (Stream st = (await File.OpenReadAsync()).AsStream())
                    using (TextReader reader = new StreamReader(st,
                                               System.Text.Encoding.GetEncoding("shift_jis")))
                    {
                        Title.Text += " [Shift-JIS Encoding]";
                        TextView.Text = await reader.ReadToEndAsync();
                    }
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
//            timer.Stop();
        }
        public void UpAction()
        {
            ScrollView.ScrollBy(0, -120);
//            repeat_scroll = -repeat_scroll_amount;
//            timer.Start();
        }
        public void DownAction()
        {
            ScrollView.ScrollBy(0, +120);
//            repeat_scroll = repeat_scroll_amount;
//            timer.Start();
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



        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
