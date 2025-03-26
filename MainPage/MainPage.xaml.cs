using APlayer.SaveData;
using APlayer.SoundPlayer;
using APlayer.StartPage;
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
using Windows.Graphics;
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

        private readonly DispatcherTimer Timer = new();

        PlayerViewModel viewModel { get; set; } = new PlayerViewModel();

        private SaveData.Folder? SavedFolder = null;
        private SaveData.List? SavedList = null;

        public class GamepadActionDelegate
        {
            public Action Up { get; set; } = Gamepad.Assign.NoAction;
            public Action Down { get; set; } = Gamepad.Assign.NoAction;
            public Action Left { get; set; } = Gamepad.Assign.NoAction;
            public Action Right { get; set; } = Gamepad.Assign.NoAction;
            public Action Select { get; set; } = Gamepad.Assign.NoAction;
        }

        private GamepadActionDelegate GamepadActions { get; set; } = new();

        private ConcurrentQueue<float> LeftPeaks = new([0, 0, 0, 0, 0]);
        private ConcurrentQueue<float> RightPeaks = new([0, 0, 0, 0, 0]);


        public MainPage()
        {
            this.InitializeComponent();

            VolumeSlider.Maximum = GainMax;
            VolumeSlider.Minimum = GainMin;

            if (App.MainWindow != null)
            {
                VolumeSliderVisible.IsChecked = App.MainWindow.DefaultVolumeSlider;
                VolumeSlider.Visibility = App.MainWindow.DefaultVolumeSlider ? Visibility.Visible : Visibility.Collapsed;
                ControlPanel.Visibility = App.MainWindow.DefaultControlPanel ? Visibility.Visible : Visibility.Collapsed;
            }

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
            if (SavedFolder != null && SavedList != null)
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(SavedFolder.Path);
                MainFrame.Navigate(typeof(FilerPage), new FilerPage.NavigationParameter(GamepadActions, SavedList, SavedFolder, folder, Frame));
            }

            var assign = App.AssignData.MainPage.CreateAssign(GetGamepadAction);
            var shifted_assign = App.AssignData.MainPageShift.CreateAssign(GetGamepadAction);
            App.Gamepad.SetAssign(assign, shifted_assign);
            App.AssignDataChanged += App_AssignDataChanged;

            App.SoundPlayer.Initialize(App.SoundDevice);

            App.SoundPlayer.PlaylistChanged += SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged += SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged += SoundPlayer_StateChanged;
            App.SoundPlayer.FrameReported += SoundPlayer_FrameReported;

            double db = ToDecibel(App.SoundPlayer.OutputGain);
            db = Math.Clamp(db, GainMin, GainMax);
            VolumeSlider.Value = db;

            App.SoundDeviceChanged += App_SoundDeviceChanged;
        }

        private void App_SoundDeviceChanged(object? sender, EventArgs e)
        {
//            App.SoundPlayer.ChangeDevice(App.SoundDevice);
        }


        private void App_AssignDataChanged(object? sender, Type e)
        {
            if (e == typeof(GamepadAssign.MainPageGamepadAction))
            {
                var assign = App.AssignData.MainPage.CreateAssign(GetGamepadAction);
                var shifted_assign = App.AssignData.MainPageShift.CreateAssign(GetGamepadAction);
                App.Gamepad.SetAssign(assign, shifted_assign);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SoundPlayer.Stop();
            App.SoundPlayer.ResetPlayList();

            //            App.Gamepad.ResetAssign();
            App.AssignDataChanged -= App_AssignDataChanged;

            App.SoundPlayer.PlaylistChanged -= SoundPlayer_PlaylistChanged;
            App.SoundPlayer.CurrentIndexChanged -= SoundPlayer_CurrentIndexChanged;
            App.SoundPlayer.StateChanged -= SoundPlayer_StateChanged;
            App.SoundPlayer.FrameReported -= SoundPlayer_FrameReported;
        }

        private Action GetGamepadAction(GamepadAssign.MainPageGamepadAction act)
        {
            return act switch
            {
                GamepadAssign.MainPageGamepadAction.Shift => Gamepad.Assign.Shift,
                GamepadAssign.MainPageGamepadAction.Backward => () => this.DispatcherQueue.TryEnqueue(StepPrev),
                GamepadAssign.MainPageGamepadAction.Forward => () => this.DispatcherQueue.TryEnqueue(StepNext),
                GamepadAssign.MainPageGamepadAction.PlayPause => () => this.DispatcherQueue.TryEnqueue(PlayPause),
                GamepadAssign.MainPageGamepadAction.GainUp => () => this.DispatcherQueue.TryEnqueue(GainUp),
                GamepadAssign.MainPageGamepadAction.GainDown => () => this.DispatcherQueue.TryEnqueue(GainDown),
                GamepadAssign.MainPageGamepadAction.Up => () => this.DispatcherQueue.TryEnqueue(ActionUp),
                GamepadAssign.MainPageGamepadAction.Down => () => this.DispatcherQueue.TryEnqueue(ActionDown),
                GamepadAssign.MainPageGamepadAction.Left => () => this.DispatcherQueue.TryEnqueue(ActionLeft),
                GamepadAssign.MainPageGamepadAction.Right => () => this.DispatcherQueue.TryEnqueue(ActionRight),
                GamepadAssign.MainPageGamepadAction.Select => () => this.DispatcherQueue.TryEnqueue(ActionSelect),
                GamepadAssign.MainPageGamepadAction.Playlist => () => this.DispatcherQueue.TryEnqueue(SwitchPlaylist),
                _ => () => { }
                ,
            };
        }
        private void ActionUp()
        {
            GamepadActions.Up.Invoke();
        }
        private void ActionDown()
        {
            GamepadActions.Down.Invoke();
        }
        private void ActionLeft()
        {
            GamepadActions.Left.Invoke();
        }
        private void ActionRight()
        {
            GamepadActions.Right.Invoke();
        }
        private void ActionSelect()
        {
            GamepadActions.Select.Invoke();
        }



        private void SoundPlayer_FrameReported(object? sender, (byte[] buffer, int length) e)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {

                float left_peak = 0;
                float right_peak = 0;
                if (App.SoundPlayer.ChannelCount == 2)
                {
                    switch (App.SoundPlayer.BitsPerSample)
                    {
                        case 16:
                            {
                                for (int i = 0; i < e.length; i += 4)
                                {
                                    int v = BitConverter.ToInt16(e.buffer, i);
                                    left_peak = Math.Max(left_peak, v / (float)-Int16.MinValue);
                                    v = BitConverter.ToInt16(e.buffer, i + 2);
                                    right_peak = Math.Max(right_peak, v / (float)-Int16.MinValue);
                                }
                            }
                            break;
                        case 24:
                            {
                                byte[] tmp = [0, 0, 0, 0];
                                for (int i = 0; i < e.length; i += 6)
                                {
                                    Array.Copy(e.buffer, i, tmp, 0, 3);
                                    tmp[3] = (byte)(0 - (tmp[2] >> 7 & 1));
                                    int v = BitConverter.ToInt32(tmp);
                                    left_peak = Math.Max(left_peak, v / (float)0x800000);
                                    Array.Copy(e.buffer, i + 3, tmp, 0, 3);
                                    tmp[3] = (byte)(0 - (tmp[2] >> 7 & 1));
                                    v = BitConverter.ToInt32(tmp);
                                    right_peak = Math.Max(right_peak, v / (float)0x800000);
                                }

                            }
                            break;
                    }
                }
                else
                {
                    for (int i = 0; i < e.length; i += 2)
                    {
                        int v = BitConverter.ToInt16(e.buffer, i);
                        left_peak = Math.Max(left_peak, v / (float)-Int16.MinValue);
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


        private void GainUp()
        {
            double db = ToDecibel(App.SoundPlayer.OutputGain);
            db = Math.Clamp(db + 1, GainMin, GainMax);
            App.SoundPlayer.OutputGain = FromDecibel(db);
            viewModel.Volume = db;
        }
        private void GainDown()
        {
            double db = ToDecibel(App.SoundPlayer.OutputGain);
            db = Math.Clamp(db - 1, GainMin, GainMax);
            App.SoundPlayer.OutputGain = FromDecibel(db);
            viewModel.Volume = db;
        }


        private void Timer_Tick(object? sender, object e)
        {
            float max_height = (float)Player.ActualHeight * 0.9f;
            var pos = App.SoundPlayer.GetPosition();
            viewModel.PlayingPosition = pos;
            //なんか AudioFileInputNode.FileCompleted が来ない時がたまにあるので、とりあえずここでチェック
            if (pos > App.SoundPlayer.CurrentTrack?.Duration)
                App.SoundPlayer.PlayNext();

            while (LeftPeaks.Count > 1)
                LeftPeaks.TryDequeue(out _);
            while (RightPeaks.Count > 1)
                RightPeaks.TryDequeue(out _);
            double l = ToDecibel(LeftPeaks.Max());
            double r = ToDecibel(RightPeaks.Max());

            viewModel.LeftPeak = (float)(Math.Clamp(l, -80, 0) + 80) / 80 * max_height;
            viewModel.RightPeak = (float)(Math.Clamp(r, -80, 0) + 80) / 80 * max_height;
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
            SwitchPlaylist();
        }

        private void SwitchPlaylist()
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

        private void ControlPanelSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (ControlPanel.Visibility == Visibility.Visible)
            {
                ControlPanel.Visibility = Visibility.Collapsed;
//                if (ControlPanelSwitch.Content is FontIcon fonticon)
//                    fonticon.Glyph = "\uE70D";
            }
            else
            {
                ControlPanel.Visibility = Visibility.Visible;
//                    fonticon.Glyph = "\uE70E";
            }
        }
    }

    partial class PlayerViewModel : INotifyPropertyChanged
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
