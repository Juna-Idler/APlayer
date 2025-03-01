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
        private IStorageFile? File = null;

        private readonly DispatcherTimer timer = new();
        private double repeat_scroll = 0;
        private ScrollingScrollOptions options = new( ScrollingAnimationMode.Auto );

        const int repeat_scroll_amount = 240;

        public TextViewPage()
        {
            this.InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(0.20);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
            ScrollView.ScrollBy(0, repeat_scroll,options);
            repeat_scroll *= 1.1;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (_, file) = ((List<FolderItem> folder, FolderItem file))e.Parameter;
            File = file.Item as IStorageFile;
        }

        void OnGamepadButtonChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons rereased) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    ScrollView.ScrollBy(0, -120);
                    repeat_scroll = -repeat_scroll_amount;
                    timer.Start();
                }
                if (e.rereased.HasFlag(XInput.Buttons.UP))
                {
                    timer.Stop();
                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    ScrollView.ScrollBy(0, +120);
                    repeat_scroll = repeat_scroll_amount;
                    timer.Start();
                }
                if (e.rereased.HasFlag(XInput.Buttons.DOWN))
                {
                    timer.Stop();
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    Frame.GoBack();
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                {
                }
                if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                {
                }
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Gamepad.Main.ButtonsChanged += OnGamepadButtonChanged;

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
            timer.Stop();
            App.Gamepad.Main.ButtonsChanged -= OnGamepadButtonChanged;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
