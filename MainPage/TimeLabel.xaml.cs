using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class TimeLabel : UserControl
    {
        public event EventHandler? TimeLabelContextRequested;

        public int MarkerTopOffset { get; set; } = 0;
        public int MarkerBottomOffset { get; set; } = 0;

        public int MarkerHeight { get => 32 - MarkerTopOffset + MarkerBottomOffset; }

        public string Label
        {
            get => LabelText.Text;
            set
            {
                LabelText.Text = value;
            }
        }
        public TimeLabel()
        {
            this.InitializeComponent();
        }


        private void Frame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetLeft(Frame, -Frame.ActualWidth / 2);
        }

        private void Frame_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            TimeLabelContextRequested?.Invoke(this, EventArgs.Empty);
            args.Handled = true;
        }
    }
}
