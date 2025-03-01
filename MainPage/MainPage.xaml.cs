using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;

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


        PlayerViewModel viewModel { get; set; } = new PlayerViewModel();

        private SaveData.Folder? SavedFolder = null;
        private SaveData.List? SavedList = null;

        private ConcurrentQueue<float> LeftPeaks = new([0,0,0,0,0]);
        private ConcurrentQueue<float> RightPeaks = new([0,0,0,0,0]);

//        private bool TriggerOn = false;

        public MainPage()
        {
            this.InitializeComponent();

            VolumeSlider.Maximum = GainMax;
            VolumeSlider.Minimum = GainMin;

            Timer.Interval = TimeSpan.FromMicroseconds(100);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            (SavedFolder, SavedList) = ((SaveData.Folder, SaveData.List))e.Parameter;
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (SavedFolder != null)
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(SavedFolder.Path);
                MainFrame.Navigate(typeof(FilerPage), (SavedList,SavedFolder, folder, Frame));
            }

            App.Gamepad.Main.ButtonsChanged += Gamepad_ButtonsChanged;
            App.Gamepad.Main.TriggerButtonsChanged += Gamepad_TriggerButtonsChanged;
            App.Gamepad.Sub.ButtonsChanged += PlayerGamePad_ButtonsChanged;
            App.Gamepad.Sub.TriggerButtonsChanged += PlayerGamePad_TriggerButtonsChanged;

            App.SoundPlayer.PlaylistChanged += SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged += SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged += SoundPlayer_StateChanged;
            App.SoundPlayer.FrameReported += SoundPlayer_PeakReported;

            double db = ToDecibel(App.SoundPlayer.OutputGain);
            db = Math.Clamp(db + 1, GainMin, GainMax);
            VolumeSlider.Value = db;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.Stop();
            App.SoundPlayer.ResetPlayList();

            App.Gamepad.Main.ButtonsChanged -= Gamepad_ButtonsChanged;
            App.Gamepad.Main.TriggerButtonsChanged -= Gamepad_TriggerButtonsChanged;
            App.Gamepad.Sub.ButtonsChanged -= PlayerGamePad_ButtonsChanged; ;
            App.Gamepad.Sub.TriggerButtonsChanged -= PlayerGamePad_TriggerButtonsChanged;

            App.SoundPlayer.PlaylistChanged -= SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged -= SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged -= SoundPlayer_StateChanged;
            App.SoundPlayer.FrameReported -= SoundPlayer_PeakReported;
        }

        private void SoundPlayer_PeakReported(object? sender, float[] e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {

                float left_peak = 0;
                float right_peak = 0;
                if (App.SoundPlayer.ChannelCount == 2)
                {
                    for (int i = 0; i < e.Length; i += 2)
                    {
                        left_peak = Math.Max(left_peak, e[i]);
                        right_peak = Math.Max(right_peak, e[i + 1]);
                    }
                }
                else
                {
                    for (int i = 0; i < e.Length; i++)
                    {
                        left_peak = Math.Max(left_peak, e[i]);
                    }
                }


                LeftPeaks.Enqueue(left_peak);
                RightPeaks.Enqueue(right_peak);
                //                viewModel.LeftPeak = (float)(Math.Clamp(e.left, -80, 0) + 80) * 1.5f;
                //                viewModel.RightPeak = (float)(Math.Clamp(e.right, -80, 0) + 80) * 1.5f;
            });
        }
        public static double ToDecibel(double linear)
        {
            return Math.Log10(linear) * 20;
        }

        public static double FromDecibel(double db)
        {
            return Math.Pow(10, db / 20);
        }


        private void Gamepad_ButtonsChanged(object? sender, (XInput.Buttons pressed, XInput.Buttons released) e)
        {
            if (sender is XInput.EventGenerator s)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.pressed.HasFlag(XInput.Buttons.BACK))
                    {
                        App.Gamepad.Sub.CopyLastStateFrom(s);
                        App.Gamepad.Main.Stop();
                        App.Gamepad.Sub.Start();
                        Player.Style = (Style)this.Resources["Controlled"];
                    }
                    if (e.pressed.HasFlag(XInput.Buttons.THUMB_LEFT))
                    {
                        if (MainFrame.SourcePageType != typeof(PlaylistPage))
                        {
                            MainFrame.Navigate(typeof(PlaylistPage));
                        }
                        else
                        {
                            if (MainFrame.CanGoBack)
                                MainFrame.GoBack();
                        }

                    }
                });
            }
        }

        private void Gamepad_TriggerButtonsChanged(object? sender, (XInput.EventGenerator.TriggerButtons pressed, XInput.EventGenerator.TriggerButtons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.pressed.HasFlag(XInput.EventGenerator.TriggerButtons.Left))
                {
//                    TriggerOn = true;
                }
                if (e.released.HasFlag(XInput.EventGenerator.TriggerButtons.Left))
                {
//                    TriggerOn = false;
                }

            });
        }

        private void PlayerGamePad_TriggerButtonsChanged(object? sender, (XInput.EventGenerator.TriggerButtons pressed, XInput.EventGenerator.TriggerButtons released) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
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
                if (e.pressed.HasFlag(XInput.Buttons.BACK))
                {
                    App.Gamepad.SwitchToMain();
                    Player.Style = (Style)this.Resources["Uncontrolled"];
                }
                if (e.pressed.HasFlag(XInput.Buttons.THUMB_LEFT))
                {
                    if (MainFrame.SourcePageType != typeof(PlaylistPage))
                    {
                        App.Gamepad.SwitchToMain();
                        Player.Style = (Style)this.Resources["Uncontrolled"];
                        MainFrame.Navigate(typeof(PlaylistPage));
                    }
                    else
                    {
                        if (MainFrame.CanGoBack)
                            MainFrame.GoBack();
                    }
                }
            });
        }


        private void Timer_Tick(object? sender, object e)
        {
            var pos = App.SoundPlayer.GetPosition();
            viewModel.PlayingPosition = pos;
            //なんか AudioFileInputNode.FileCompleted が来ない時がたまにあるので、とりあえずここでチェック
            if (pos > App.SoundPlayer.CurrentTrack?.Duration)
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

        private void SoundPlayer_PlaylistChanged(object? sender, (IReadOnlyList<ISoundPlayer.ITrack> list, int index) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
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
                    viewModel.PlayingTitle = e.list[e.index].Name;
                    viewModel.PlayingPosition = TimeSpan.Zero;
                    viewModel.Duration = e.list[e.index].Duration;
                }
            });
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
                    viewModel.PlayingTitle = viewModel.Playlist[e].Name;
                    viewModel.PlayingPosition = TimeSpan.Zero;
                    viewModel.Duration = viewModel.Playlist[e].Duration;
                }
            });
        }
        private void SoundPlayer_StateChanged(object? sender, ISoundPlayer.PlayerState e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                viewModel.StateButton = e switch
                {
                    ISoundPlayer.PlayerState.Playing => Symbol.Pause,
                    ISoundPlayer.PlayerState.Paused => Symbol.Play,
                    ISoundPlayer.PlayerState.Stoped => Symbol.Play,
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
            if (App.SoundPlayer.State == ISoundPlayer.PlayerState.Playing)
                App.SoundPlayer.Pause();
            else if (App.SoundPlayer.State != ISoundPlayer.PlayerState.Null)
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
            var d = App.SoundPlayer.CurrentTrack?.Duration;
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

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            VolumeSlider.Visibility = Visibility.Visible;
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            VolumeSlider.Visibility = Visibility.Collapsed;
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.SourcePageType != typeof(PlaylistPage))
            {
                if (App.Gamepad.IsSubPolling)
                {
                    App.Gamepad.SwitchToMain();
                    Player.Style = (Style)this.Resources["Uncontrolled"];
                }
                MainFrame.Navigate(typeof(PlaylistPage));
            }
            else
            {
                if (App.Gamepad.IsSubPolling)
                {
                    App.Gamepad.SwitchToMain();
                    Player.Style = (Style)this.Resources["Uncontrolled"];
                }
                if (MainFrame.CanGoBack)
                    MainFrame.GoBack();
            }
        }
    }

    class PlayerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IReadOnlyList<ISoundPlayer.ITrack> Playlist { get; set; } = [];
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
        {
            get => ((int)playingPosition.TotalMinutes).ToString() + playingPosition.ToString(@"\:ss\.ff");
        }

        public string DurationString
        {
            get => Math.Floor(duration.TotalMinutes).ToString("F0") + duration.ToString(@"\:ss\.ff");
        }

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
        public string VolumeNumber { get => volume.ToString("F2") + "dB"; }


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
