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
using Windows.Media.Audio;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistPage : Page
    {
        private ObservableCollection<PlaylistItem> List = [];

        private List<IStorageFile> fileList = [];
        private int fileListIndex = 0;

        public PlaylistPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            fileList = [];
            fileListIndex = -1;

            if (e.Parameter != null)
            {
                var (folder, file) = ((List<FolderItem> folder, FolderItem file))e.Parameter;

                fileListIndex = App.SoundPlayer.Playlist.FindIndex(item => item.SourceFile == file.Item as StorageFile);
                if (fileListIndex == -1)
                {
                    fileList = new(folder
                        .Where(item => item.Type == FolderItem.ItemType.Audio)
                        .Select(item => (IStorageFile)item.Item));
                    fileListIndex = fileList.FindIndex(0, item => item.Name == file.Item.Name);
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (fileList.Count > 0)
            {
                await App.SoundPlayer.SetPlayList(fileList, fileListIndex);
                App.SoundPlayer.Play();
            }
            if (fileListIndex >= 0)
            {
                App.SoundPlayer.PlayIndex(fileListIndex);
            }
            List = new(App.SoundPlayer.Playlist.Select(item => new PlaylistItem(item)));
            PlaylistView.ItemsSource = List;

            int index = App.SoundPlayer.CurrentIndex;
            for (int i = 0; i < List.Count; i++)
            {
                List[i].IsPlaying = i == index;
            }
            PlaylistView.SelectedIndex = index;
            App.SoundPlayer.CurrentIndexChanged += SoundPlayer_CurrentIndexChanged;
            App.Gamepad.ButtonsChanged += Gamepad_ButtonsChanged;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.CurrentIndexChanged -= SoundPlayer_CurrentIndexChanged;
            App.Gamepad.ButtonsChanged -= Gamepad_ButtonsChanged;
        }


        private void Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    if (PlaylistView.SelectedIndex > 0)
                        PlaylistView.SelectedIndex--;
                    else
                        PlaylistView.SelectedIndex = List.Count - 1;
                    PlaylistView.ScrollIntoView(PlaylistView.SelectedItem);
                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    if (PlaylistView.SelectedIndex < List.Count - 1)
                        PlaylistView.SelectedIndex++;
                    else
                        PlaylistView.SelectedIndex = 0;
                    PlaylistView.ScrollIntoView(PlaylistView.SelectedItem);
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    Frame.GoBack();
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                { }
                if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                {
                    App.SoundPlayer.PlayIndex(PlaylistView.SelectedIndex);
                }
            });
        }

        private void SoundPlayer_CurrentIndexChanged(object? sender, int e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                int index = App.SoundPlayer.CurrentIndex;
                for (int i = 0; i < List.Count; i++)
                {
                    List[i].IsPlaying = i == index;
                }
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }

    public class PlaylistItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PlaylistItem(AudioFileInputNode track,string title = "")
        {
            Track = track;
            Title = title != "" ? title : track.SourceFile.Name;
        }
        public AudioFileInputNode Track { get; private set; }

        private bool isPlaying;
        public bool IsPlaying
        {
            get => isPlaying;
            set {
                isPlaying = value;
                NotifyPropertyChanged(nameof(Visibility));
                NotifyPropertyChanged(nameof(Playing));
            }
        }
        public Visibility Visibility { get => IsPlaying ? Visibility.Visible : Visibility.Collapsed; }
        public string Playing { get => IsPlaying ? "Playing" : ""; }
        public string Title { get; private set; }
        public string Duration { get => Track.Duration.ToString(@"hh\:mm\:ss");}
        public string Codec { get => Track.EncodingProperties.Subtype; }
        public uint Bitrate { get => Track.EncodingProperties.Bitrate;}
    }
}
