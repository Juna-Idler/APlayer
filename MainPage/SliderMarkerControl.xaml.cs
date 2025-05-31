using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class SliderMarkerControl : UserControl
    {
        public event EventHandler<TimeSpan[]>? Updated;

        public int MarkerTopOffset { get; set; } = 20;
        public int MarkerBottomOffset { get; set; } = 24;

        public int LabelOffset { get; set; } = 8;
        public int EdittingLabelOffset { get; set; } = -16;

        public TimeSpan Duration
        {
            get => TimeSpan.FromSeconds(MarkerSlider.Maximum);
            set => MarkerSlider.Maximum = value.TotalSeconds;
        }

        class Mark(TimeSpan time, MenuFlyoutItem menuItem, APlayer.TimeLabel timeLabel)
        {
            public TimeSpan Time = time;
            public MenuFlyoutItem MenuItem = menuItem;

            public APlayer.TimeLabel TimeLabel = timeLabel;
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
            var label = new APlayer.TimeLabel
            {
                Label = "0:00",
                MarkerTopOffset = MarkerTopOffset,
                MarkerBottomOffset = MarkerBottomOffset,
            };
            label.TimeLabelContextRequested += Label_TimeLabelContextRequested;
            Canvas.SetLeft(label, OffsetLeft);
            MarkerCanvas.Children.Add(label);

            var mark = new Mark(TimeSpan.Zero, menu, label);
            Marks.Add(mark);

            Editing = mark;
            MarkerSlider.Value = 0;
            MarkerSlider.Visibility = Visibility.Visible;
            foreach (var item in Marks)
            {
                Canvas.SetTop(item.TimeLabel, EdittingLabelOffset);
            }
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
                var label = new TimeLabel
                {
                    Label = menu.Text,
                    MarkerTopOffset = MarkerTopOffset,
                    MarkerBottomOffset = MarkerBottomOffset,
                };
                label.TimeLabelContextRequested += Label_TimeLabelContextRequested;
                var rate = (mark.TotalSeconds - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);
                Canvas.SetLeft(label, (ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
                Canvas.SetTop(label, LabelOffset);
                MarkerCanvas.Children.Add(label);
                Marks.Add(new Mark(mark, menu, label));
            }

        }

        private void Label_TimeLabelContextRequested(object? sender, EventArgs e)
        {
            if (Editing != null)
            {
                UnshiftEditMode();
                return;
            }
            if (sender is TimeLabel label)
            {
                var find = Marks.Find((item) => { return item.TimeLabel == label; });
                if (find != null)
                {
                    ShiftEditMode(find);
                }
            }
        }
        private void ShiftEditMode(Mark mark)
        {
            Editing = mark;
            MarkerSlider.Value = Editing.Time.TotalSeconds;
            MarkerSlider.Visibility = Visibility.Visible;

            foreach (var item in Marks)
            {
                Canvas.SetTop(item.TimeLabel, EdittingLabelOffset);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var find = Marks.Find((item) => { return item.MenuItem == sender as MenuFlyoutItem; });
            if (find != null)
            {
                ShiftEditMode(find);
            }
        }


        private void MarkerSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Editing != null)
            {
                var val = Math.Clamp(e.NewValue, MarkerSlider.Minimum, MarkerSlider.Maximum);
                Editing.Time = TimeSpan.FromSeconds(val);
                var rate = (val - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);

                Canvas.SetLeft(Editing.TimeLabel, (ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
                Editing.TimeLabel.Label = TimeSliderValueConverter.Convert(Editing.Time);
            }
        }

        private void MarkerSlider_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            UnshiftEditMode();
            args.Handled = true;
        }

        private void UnshiftEditMode()
        {
            if (Editing != null)
            {
                if (Editing.Time == TimeSpan.Zero)
                {
                    MenuFlyout.Items.Remove(Editing.MenuItem);
                    MarkerCanvas.Children.Remove(Editing.TimeLabel);
                    Marks.Remove(Editing);
                }
                else
                {
                    if (Editing.MenuItem.Text == "New")
                    {
                        Editing.MenuItem.Click += Edit_Click;
                        MenuFlyout.Items.Add(Editing.MenuItem);
                    }
                    Editing.MenuItem.Text = TimeSliderValueConverter.Convert(Editing.Time);
                }
                Updated?.Invoke(this, [.. Marks.Select(e => e.Time)]);
                Editing = null;
                foreach (var item in Marks)
                {
                    Canvas.SetTop(item.TimeLabel, LabelOffset);
                }
            }
            MarkerSlider.Visibility = Visibility.Collapsed;
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
                var val = Math.Clamp(mark.Time.TotalSeconds, MarkerSlider.Minimum, MarkerSlider.Maximum);
                var rate = (val - MarkerSlider.Minimum) / (MarkerSlider.Maximum - MarkerSlider.Minimum);

                Canvas.SetLeft(mark.TimeLabel, (ActualWidth - OffsetLeft - OffsetRight) * rate + OffsetLeft);
            }

        }

        private void MarkerSlider_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pp = e.GetCurrentPoint((UIElement)sender);
            if (pp.Properties.MouseWheelDelta > 0)
            {
                MarkerSlider.Value += 1;
            }
            else
            {
                MarkerSlider.Value -= 1;
            }
        }
    }
}
