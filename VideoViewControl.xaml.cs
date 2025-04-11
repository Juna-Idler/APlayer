using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Playback;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    public sealed partial class VideoViewControl : UserControl
    {
        public event EventHandler? RequestedClose;

        public VideoViewControl()
        {
            this.InitializeComponent();

            MPElement.TransportControls.IsSkipBackwardEnabled = true;
            MPElement.TransportControls.IsSkipBackwardButtonVisible = true;
            MPElement.TransportControls.IsSkipForwardEnabled = true;
            MPElement.TransportControls.IsSkipForwardButtonVisible = true;

            MPElement.TransportControls.IsRepeatEnabled = true;
            MPElement.TransportControls.IsRepeatButtonVisible = true;
        }
        public async Task SetAudioDevice(string? device_name)
        {
            if (device_name == null)
            {
                var device = await DeviceInformation.CreateFromIdAsync(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default));
                MPElement.MediaPlayer.AudioDevice = device;
                return;
            }

            var devices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());
            foreach (var device in devices)
            {
                if (device.Name == device_name)
                {
                    MPElement.MediaPlayer.AudioDevice = device;
                    return;
                }
            }
            {
                var device = await DeviceInformation.CreateFromIdAsync(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default));
                MPElement.MediaPlayer.AudioDevice = device;
            }
        }


        public bool LoadVideo(IStorageFile file)
        {
            if (!file.ContentType.StartsWith("video"))
                return false;

            try
            {
                MPElement.Source = MediaSource.CreateFromStorageFile(file);
            }
            catch
            {
                return false;
            }
            MPElement.MediaPlayer.AudioCategory = Windows.Media.Playback.MediaPlayerAudioCategory.Media;
            return true;
        }

        public void Play()
        {
            MPElement.MediaPlayer.Play();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MPElement.MediaPlayer.Pause();
            MPElement.Source = null;
            RequestedClose?.Invoke(this, EventArgs.Empty);
        }

    }
}
