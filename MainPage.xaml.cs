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
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer Timer = new();

        private string rootFolderPath = "";
        private StorageFolder? rootFolder = null;

        PlayerViewModel viewModel { get; set; } = new PlayerViewModel();

        public MainPage()
        {
            this.InitializeComponent();

            App.SoundPlayer.PlaylistChanged += SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged += SoundPlayer_CurrentIndexChanged;

            Timer.Interval = TimeSpan.FromMicroseconds(100);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        private void Timer_Tick(object? sender, object e)
        {
            var pos = App.SoundPlayer.GetPosition();
            viewModel.PlayingPosition = pos.TotalMilliseconds;
        }

        private void SoundPlayer_PlaylistChanged(object? sender, (IReadOnlyList<Windows.Media.Audio.AudioFileInputNode> list, int index) e)
        {
            viewModel.Playlist = e.list;
            viewModel.CurrentPlaylistIndex = e.index;
            viewModel.PlayingTitle = e.list[e.index].SourceFile.Name;
            viewModel.Duration = e.list[e.index].Duration.TotalMilliseconds;
        }

        private void SoundPlayer_CurrentIndexChanged(object? sender, int e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                viewModel.CurrentPlaylistIndex = e;
                viewModel.PlayingTitle = viewModel.Playlist[e].SourceFile.Name;
                viewModel.Duration = viewModel.Playlist[e].Duration.TotalMilliseconds;
            });
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var param = e.Parameter as SavedFolder;
            if (param != null)
            {
                rootFolderPath = param.Path;
                rootFolder = await StorageFolder.GetFolderFromPathAsync(param.Path);

                MainFrame.Navigate(typeof(FilerPage),rootFolder);
            }
        }

        private void SkipPrev_Click(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.PreviousPlay();
        }
        private void StepPrev_Click(object sender, RoutedEventArgs e)
        {
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (App.SoundPlayer.State == SoundPlayer.PlayerState.Playing)
                App.SoundPlayer.Pause();
            else if (App.SoundPlayer.State != SoundPlayer.PlayerState.Null)
            {
                App.SoundPlayer.Play();
            }
            if (App.SoundPlayer.State == SoundPlayer.PlayerState.Playing)
            {
                PlayPause.Content = "⏸";
            }
            else
            {
                PlayPause.Content = "⏯";//▶
            }
        }

        private void StepNext_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SkipNext_Click(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.NextPlay();
        }
    }

    class PlayerViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IReadOnlyList<Windows.Media.Audio.AudioFileInputNode> Playlist { get; set; } = [];
        public int CurrentPlaylistIndex { get; set; } = 0;

        private string playingTitle = string.Empty;
        public string PlayingTitle
        {
            get => playingTitle; set
            {
                playingTitle = value;
                NotifyPropertyChanged();
            }
        }

        private double playingPosition;
        public double PlayingPosition
        {
            get => playingPosition;
            set
            {
                playingPosition = value;
                NotifyPropertyChanged();
            }
        }

        private double duration;
        public double Duration
        {
            get => duration;
            set
            {
                duration = value;
                NotifyPropertyChanged();
            }
        }

    }
}
