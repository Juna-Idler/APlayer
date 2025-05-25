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
using Microsoft.UI;
using Windows.UI.Input.Inking;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class SliderMarkerControl : UserControl
    {
        public int MarkerTopOffset { get; set; } = 0;
        public int MarkerBottomOffset { get; set; } = 0;

        public TimeSpan Duration
        {
            get => TimeSpan.FromSeconds(MarkerSlider.Maximum);
            set => MarkerSlider.Maximum = value.TotalSeconds;
        }

        class Mark(int seconds, MenuFlyoutItem menuItem, Microsoft.UI.Xaml.Shapes.Rectangle rectangle)
        {
            public int Seconds = seconds;
            public MenuFlyoutItem MenuItem = menuItem;
            public Microsoft.UI.Xaml.Shapes.Rectangle Rectangle = rectangle;
        }

        private double OffsetLeft = 0;
        private double OffsetRight = 0;

        private readonly MenuFlyoutItem NewMark;

        private readonly List<Mark> Marks = [];
        private Mark? Editing = null;


        public SliderMarkerControl()
        {
            this.InitializeComponent();

            NewMark = new MenuFlyoutItem();
            NewMark.Text = "New Mark";
            NewMark.Click += NewMark_Click;
            MenuFlyout.Items.Add(NewMark);

            MarkerSlider.ThumbToolTipValueConverter = new TimeSliderValueConverter();
        }
        private void NewMark_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuFlyoutItem();
            menu.Text = "New";
            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = 2,
                Height = 32 - MarkerTopOffset + MarkerBottomOffset,
                Fill = new SolidColorBrush((Windows.UI.Color)Resources["SystemAccentColor"])
            };
            Canvas.SetTop(rect,MarkerTopOffset);
            Canvas.SetLeft(rect, OffsetLeft);
            MarkerCanvas.Children.Add(rect);
            var mark = new Mark(0, menu, rect);
            Marks.Add(mark);

            Editing = mark;
            MarkerSlider.Value = 0;
            MarkerSlider.Visibility = Visibility.Visible;
        }

        public void SetMarks(TimeSpan[] marks, TimeSpan duration)
        {
            Editing = null;
            MarkerSlider.Visibility = Visibility.Collapsed;

            MenuFlyout.Items.Clear();
            MenuFlyout.Items.Add(NewMark);
            MarkerCanvas.Children.Clear();
            Marks.Clear();

            Duration = duration;
            foreach (var mark in marks)
            {
                var menu = new MenuFlyoutItem
                {
                    Text = TimeSliderValueConverter.Convert(mark.TotalSeconds)
                };
                menu.Click += Edit_Click;
                MenuFlyout.Items.Add(menu);
                var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = 2,
                    Height = 32 - MarkerTopOffset + MarkerBottomOffset,
                    Fill = new SolidColorBrush((Windows.UI.Color)Resources["SystemAccentColor"])
                };
                Canvas.SetTop(rect, MarkerTopOffset);
                var rate = (mark.TotalSeconds - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);
                Canvas.SetLeft(rect, (MarkerSlider.ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
                MarkerCanvas.Children.Add(rect);
                Marks.Add(new Mark(((int)mark.TotalSeconds), menu, rect));
            }

        }
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var find = Marks.Find((item) => { return item.MenuItem == sender as MenuFlyoutItem; });
            if (find != null)
            {
                Editing = find;
                MarkerSlider.Value = Editing.Seconds;
                MarkerSlider.Visibility = Visibility.Visible;
            }
        }



        private void MarkerSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Editing != null)
            {
                var val = Math.Clamp(e.NewValue, MarkerSlider.Minimum, MarkerSlider.Maximum);
                Editing.Seconds = (int)val;
                var rate = (val - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);

                Canvas.SetLeft(Editing.Rectangle, (MarkerSlider.ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
            }

        }

        private void MarkerSlider_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            if (Editing != null)
            {
                if (Editing.Seconds == 0)
                {
                    MenuFlyout.Items.Remove(Editing.MenuItem);
                    MarkerCanvas.Children.Remove(Editing.Rectangle);
                    Marks.Remove(Editing);
                }
                else
                {
                    if (Editing.MenuItem.Text == "New")
                    {
                        Editing.MenuItem.Click += Edit_Click;
                        MenuFlyout.Items.Add(Editing.MenuItem);
                    }
                    Editing.MenuItem.Text = TimeSliderValueConverter.Convert(Editing.Seconds);
                }
                Editing = null;
            }
            MarkerSlider.Visibility = Visibility.Collapsed;
            args.Handled = true;

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var min_thumb = GetElementFromParent(SliderMin, typeof(Thumb)) as Thumb;
            var max_thumb = GetElementFromParent(SliderMax, typeof(Thumb)) as Thumb;

            if (min_thumb != null)
            {
                OffsetLeft = min_thumb.ActualOffset.X + min_thumb.ActualSize.X / 2 - 1;
            }
            if (max_thumb != null)
            {
                OffsetRight = SliderMax.ActualWidth - (max_thumb.ActualOffset.X + max_thumb.ActualSize.X / 2 - 1);
            }

            SliderMin.Visibility = Visibility.Collapsed;
            SliderMax.Visibility = Visibility.Collapsed;

        }

        private static DependencyObject? GetElementFromParent(DependencyObject parent, Type type)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement childframeworkelement && childframeworkelement.GetType() == type)
                    return child;

                var FindRes = GetElementFromParent(child, type);
                if (FindRes != null)
                    return FindRes;
            }
            return null;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var mark in Marks)
            {
                var val = Math.Clamp(mark.Seconds, MarkerSlider.Minimum, MarkerSlider.Maximum);
                var rate = (val - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);

                Canvas.SetLeft(mark.Rectangle, (ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
            }

        }

    }
}
