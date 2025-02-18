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
using Windows.UI;
using System.Collections.Concurrent;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const double GainMax = 10;
        const double GainMin = -40;

        private DispatcherTimer Timer = new();

        private string rootFolderPath = "";
        private StorageFolder? rootFolder = null;

        PlayerViewModel viewModel { get; set; } = new PlayerViewModel();

        private ConcurrentQueue<float> LeftPeaks = new([0,0,0,0,0]);
        private ConcurrentQueue<float> RightPeaks = new([0,0,0,0,0]);

        readonly XInput.EventGenerator PlayerGamePad = new(0, TimeSpan.FromMilliseconds(16));
        bool changing_interval = false;
        readonly DispatcherTimer ci_timer = new() { Interval = TimeSpan.FromSeconds(0.5) };

        public MainPage()
        {
            ci_timer.Tick += (s, o) => { changing_interval = false;ci_timer.Stop(); };
            this.InitializeComponent();

            VolumeSlider.Maximum = GainMax;
            VolumeSlider.Minimum = GainMin;

            Timer.Interval = TimeSpan.FromMicroseconds(100);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var param = e.Parameter as SavedFolder;
            if (param != null)
            {
                rootFolderPath = param.Path;
            }
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rootFolder = await StorageFolder.GetFolderFromPathAsync(rootFolderPath);
            MainFrame.Navigate(typeof(FilerPage), (rootFolder, Frame));

            App.Gamepad.TriggerButtonsChanged += Gamepad_TriggerButtonsChanged;
            PlayerGamePad.ButtonsChanged += PlayerGamePad_ButtonsChanged; ;
            PlayerGamePad.TriggerButtonsChanged += PlayerGamePad_TriggerButtonsChanged;

            App.SoundPlayer.PlaylistChanged += SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged += SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged += SoundPlayer_StateChanged;
            App.SoundPlayer.PeakReported += SoundPlayer_PeakReported;

            VolumeSlider.Value = App.SoundPlayer.OutputGain;
        }


        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.Stop();
            App.SoundPlayer.ResetPlayList();

            App.Gamepad.TriggerButtonsChanged -= Gamepad_TriggerButtonsChanged;
            PlayerGamePad.ButtonsChanged -= PlayerGamePad_ButtonsChanged; ;
            PlayerGamePad.TriggerButtonsChanged -= PlayerGamePad_TriggerButtonsChanged;

            App.SoundPlayer.PlaylistChanged -= SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged -= SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged -= SoundPlayer_StateChanged;
            App.SoundPlayer.PeakReported -= SoundPlayer_PeakReported;
        }

        private void SoundPlayer_PeakReported(object? sender, (float left, float right) e)
        {
            LeftPeaks.Enqueue(e.left);
            RightPeaks.Enqueue(e.right);
//            this.DispatcherQueue.TryEnqueue(() =>
//            {
//                viewModel.LeftPeak = (float)(ToDecibel(e.left) + 80) * 2;
//                viewModel.RightPeak = (float)(ToDecibel(e.right) + 80) * 2;
//                viewModel.LeftPeak = (float)(e.left) * 160;
//                viewModel.RightPeak = (float)(e.right) * 160;
//            });
        }
        public static double ToDecibel(double linear)
        {
            return Math.Log10(linear) * 20;
        }

        public static double FromDecibel(double db)
        {
            return Math.Pow(10, db / 20);
        }



        private void Gamepad_TriggerButtonsChanged(object? sender, (XInput.EventGenerator.TriggerButtons pressed, XInput.EventGenerator.TriggerButtons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.EventGenerator.TriggerButtons.Left) && !changing_interval)
                {
                    App.Gamepad.Stop();
                    PlayerGamePad.Start();
                    Player.Style = (Style)this.Resources["Controlled"];
                    changing_interval = true;
                    ci_timer.Start();
                }
            });
        }

        private void PlayerGamePad_TriggerButtonsChanged(object? sender, (XInput.EventGenerator.TriggerButtons pressed, XInput.EventGenerator.TriggerButtons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.EventGenerator.TriggerButtons.Left) && !changing_interval)
                {
                    PlayerGamePad.Stop();
                    App.Gamepad.Start();
                    Player.Style = (Style)this.Resources["Uncontrolled"];
                    changing_interval = true;
                    ci_timer.Start();
                }
            });
        }

        private void PlayerGamePad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.Buttons.UP))
                {
                    double db = ToDecibel(App.SoundPlayer.OutputGain);
                    db = Math.Clamp(db + 1, GainMin, GainMax);
                    App.SoundPlayer.OutputGain = FromDecibel(db);
                    viewModel.Volume = db;
                }
                if (e.pressed.HasFlag(XInput.Buttons.DOWN))
                {
                    double db = ToDecibel(App.SoundPlayer.OutputGain);
                    db = Math.Clamp(db - 1, GainMin, GainMax);
                    App.SoundPlayer.OutputGain = FromDecibel(db);
                    viewModel.Volume = db;
                }
                if (e.pressed.HasFlag(XInput.Buttons.LEFT))
                {
                    StepPrev();
                }
                if (e.pressed.HasFlag(XInput.Buttons.RIGHT))
                {
                    StepNext();
                }
                if (e.pressed.HasFlag(XInput.Buttons.SHOULDER_LEFT))
                {
                    PlayPause();
                }
            });
        }


        private void Timer_Tick(object? sender, object e)
        {
            var pos = App.SoundPlayer.GetPosition();
            viewModel.PlayingPosition = pos;
            //なんか AudioFileInputNode.FileCompleted が来ない時がたまにあるので、とりあえずここでチェック
            if (pos > App.SoundPlayer.GetCurrentDuration())
                App.SoundPlayer.PlayNext();

            while (LeftPeaks.Count > 5)
                LeftPeaks.TryDequeue(out _);
            while (RightPeaks.Count > 5)
                RightPeaks.TryDequeue(out _);
            double l = ToDecibel(LeftPeaks.Max());
            double r = ToDecibel(RightPeaks.Max());

            viewModel.LeftPeak = (float)(Math.Clamp(l, -80, 0) + 80) * 1.5f;
            viewModel.RightPeak = (float)(Math.Clamp(r, -80, 0) + 80) * 1.5f;
        }

        private void SoundPlayer_PlaylistChanged(object? sender, (IReadOnlyList<Windows.Media.Audio.AudioFileInputNode> list, int index) e)
        {
            viewModel.Playlist = e.list;
            viewModel.CurrentPlaylistIndex = e.index;
            if (e.index < 0)
            {
                viewModel.PlayingTitle = "";
                viewModel.PlayingPosition = TimeSpan.Zero;
                viewModel.Duration = TimeSpan.Zero;
            }
            else
            {
                viewModel.PlayingTitle = e.list[e.index].SourceFile.Name;
                viewModel.PlayingPosition = TimeSpan.Zero;
                viewModel.Duration = e.list[e.index].Duration;
            }
        }

        private void SoundPlayer_CurrentIndexChanged(object? sender, int e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                viewModel.CurrentPlaylistIndex = e;
                if (e < 0)
                {
                    viewModel.PlayingTitle = "";
                    viewModel.PlayingPosition = TimeSpan.Zero;
                    viewModel.Duration = TimeSpan.Zero;
                }
                else
                {
                    viewModel.PlayingTitle = viewModel.Playlist[e].SourceFile.Name;
                    viewModel.PlayingPosition = TimeSpan.Zero;
                    viewModel.Duration = viewModel.Playlist[e].Duration;
                }
            });
        }
        private void SoundPlayer_StateChanged(object? sender, SoundPlayer.PlayerState e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                viewModel.StateButton = e switch
                {
                    SoundPlayer.PlayerState.Playing => Symbol.Pause,
                    SoundPlayer.PlayerState.Paused => Symbol.Play,
                    SoundPlayer.PlayerState.Stoped => Symbol.Play,
                    _ => Symbol.Stop,
                };
            });
        }

        private void SkipPrev_Click(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.PlayPrevious();
        }
        private void StepPrev_Click(object sender, RoutedEventArgs e)
        {
            StepPrev();
        }
        private static void StepPrev()
        {
            var pos = App.SoundPlayer.GetPosition();
            pos -= TimeSpan.FromSeconds(10);
            if (pos < TimeSpan.Zero)
                pos = TimeSpan.Zero;
            App.SoundPlayer.Seek(pos);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }
        private void PlayPause()
        {
            if (App.SoundPlayer.State == SoundPlayer.PlayerState.Playing)
                App.SoundPlayer.Pause();
            else if (App.SoundPlayer.State != SoundPlayer.PlayerState.Null)
            {
                App.SoundPlayer.Play();
            }
        }

        private void StepNext_Click(object sender, RoutedEventArgs e)
        {
            StepNext();
        }
        private void StepNext()
        {
            var d = App.SoundPlayer.GetCurrentDuration();
            if (d == null)
                return;
            var pos = App.SoundPlayer.GetPosition();
            pos += TimeSpan.FromSeconds(10);
            if (pos >= d)
                App.SoundPlayer.PlayNext();
            else
                App.SoundPlayer.Seek(pos);
        }

        private void SkipNext_Click(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.PlayNext();
        }

        private void PlayingPosition_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue == viewModel.PlayingPosition.TotalSeconds)
                return;
            App.SoundPlayer.Seek(TimeSpan.FromSeconds(e.NewValue));
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue == viewModel.Volume)
                return;
            App.SoundPlayer.OutputGain = FromDecibel(e.NewValue);
            viewModel.Volume = e.NewValue;
        }
    }

    class PlayerViewModel : INotifyPropertyChanged
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
        private Symbol stateButton = Symbol.Stop;
        public Symbol StateButton
        {
            get => stateButton;
            set
            {
                stateButton = value;
                NotifyPropertyChanged();
            }
        }


        private TimeSpan playingPosition;
        public TimeSpan PlayingPosition
        {
            get => playingPosition;
            set
            {
                playingPosition = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PlayingPositionString));
            }
        }

        private TimeSpan duration;

        public TimeSpan Duration
        {
            get => duration;
            set
            {
                duration = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DurationString));
            }
        }

        public string PlayingPositionString
            { get => string.Format("{0}",(int)playingPosition.TotalMinutes) + playingPosition.ToString(@"\:ss\.ff"); }

        public string DurationString
        { get => ((int)duration.TotalMinutes).ToString("0") + duration.ToString(@"\:ss\.ff"); }


        private double volume;
        public double Volume
        {
            get => volume;
            set
            {
                volume = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(VolumeNumber));
            }
        }
        public string VolumeNumber { get => volume.ToString("F2"); }


        private float leftPeak;
        public float LeftPeak
        {
            get => leftPeak;
            set
            {
                leftPeak = value;
                NotifyPropertyChanged();
            }
        }
        private float rightPeak;
        public float RightPeak
        {
            get => rightPeak;
            set
            {
                rightPeak = value;
                NotifyPropertyChanged();
            }
        }
    }
}
