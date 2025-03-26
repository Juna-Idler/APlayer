using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static APlayer.SoundPlayer.ISoundPlayer;
using Windows.Storage;

namespace APlayer.SoundPlayer
{
    public class WasapiSharedPlayer : ISoundPlayer
    {
        public const string PlayerName = "WASAPI Shared";
        public string Name { get => PlayerName; }

        public event EventHandler<(IReadOnlyList<ITrack> list, int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;
        public event EventHandler<PlayerState>? StateChanged;
        public event EventHandler<(byte[], int)>? FrameReported;


        public class Device(MMDevice device) : IDevice
        {
            public string Name { get; set; } = device.FriendlyName;
            public MMDevice MMDevice { get; set; } = device;
        }
        public IDevice[] GetDevices()
        {
            MMDeviceEnumerator enumerator = new();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            return [.. endpoints.Select(d => new Device(d))];
        }


        private class Track(string file_path) : ITrack
        {
            public WaveStream Reader { get; set; } = new MediaFoundationReader(file_path);

            public string Name { get => System.IO.Path.GetFileName(file_path); }
            public string Path { get => file_path; }

            public TimeSpan Duration { get => Reader.TotalTime; }
        }

        public const int Latency = 200;

        private Device? outputDevice = null;

        public IDevice? OutputDevice => outputDevice;

        private WasapiOut? WasapiOutput = null;

        private ReplaceableWaveProvider? Provider = null;

        public uint SampleRate => (WasapiOutput == null) ? 0 : (uint)WasapiOutput.OutputWaveFormat.SampleRate;
        public uint BitsPerSample => (WasapiOutput == null) ? 0 : (uint)WasapiOutput.OutputWaveFormat.BitsPerSample;

        public uint ChannelCount => (WasapiOutput == null) ? 0 : (uint)WasapiOutput.OutputWaveFormat.Channels;


        public PlayerState State { get; private set; } = PlayerState.Null;

        private Track? currentTrack = null;
        public ITrack? CurrentTrack { get => currentTrack; }

        private List<Track> playlist = [];
        public IReadOnlyList<ITrack> Playlist { get => playlist; }

        public void InsertPlaylist(int index, ITrack track)
        {
            if (track is Track t)
                playlist.Insert(index, t);
        }
        public void RemovePlaylist(ITrack track)
        {
            if (track is Track t)
                playlist.Remove(t);
        }
        public void RemoveAtPlaylist(int index)
        {
            playlist.RemoveAt(index);
        }

        public int CurrentIndex
        {
            get => playlist.FindIndex(item => item == CurrentTrack);
        }

        public double OutputGain
        {
            get
            {
                if (WasapiOutput == null)
                    return 0;
                return WasapiOutput.Volume;
            }
            set
            {
                if (WasapiOutput != null)
                {
                    WasapiOutput.Volume = (float)Math.Clamp(value, 0, 1.0);
                }
            }
        }

        private readonly Stopwatch Stopwatch = new();
        private TimeSpan BaseTime = TimeSpan.Zero;

        ~WasapiSharedPlayer()
        {
            Terminalize();
        }

        public bool Initialize(ISoundPlayer.IDevice? device = null)
        {
            Terminalize();
            outputDevice = device as Device;
            if (CreateWasapi())
            {
                State = PlayerState.Empty;
                return true;
            }
            return false;
        }
        private bool CreateWasapi()
        {
            if (outputDevice != null)
            {
                WasapiOutput = new WasapiOut(outputDevice.MMDevice, AudioClientShareMode.Shared, true, Latency);
            }
            else
            {
                WasapiOutput = new WasapiOut(AudioClientShareMode.Shared, true, Latency);
            }
            return WasapiOutput != null;
        }

        public void Terminalize()
        {
            ResetPlayList();
            Provider?.ResetAudioFile();
            Provider = null;
            WasapiOutput?.Dispose();
            WasapiOutput = null;
            State = PlayerState.Null;
        }



        public void SetPlaylist(IEnumerable<IStorageFile> list, int index = 0)
        {
            if (WasapiOutput == null)
                return;


            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            foreach (var item in playlist)
            {
                item.Reader.Dispose();
            }
            playlist.Clear();
            foreach (var item in list)
            {
                try
                {
                    var track = new Track(item.Path);
                    playlist.Add(track);
                }
                catch { }
            }
            if (Playlist.Count == 0)
            {
                if (State != PlayerState.Empty)
                    StateChanged?.Invoke(this, PlayerState.Empty);
                State = PlayerState.Empty;
                currentTrack = null;
                PlaylistChanged?.Invoke(this, (Playlist, -1));
            }
            else
            {
                State = PlayerState.Stoped;
                int i = Math.Clamp(index, 0, Playlist.Count - 1);
                currentTrack = playlist[i];
                PlaylistChanged?.Invoke(this, (Playlist, i));
            }

        }
        public void ResetPlayList()
        {
            if (WasapiOutput == null)
                return;
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            currentTrack = null;
            foreach (var item in playlist)
            {
                item.Reader.Dispose();
            }
            playlist.Clear();

            if (State != PlayerState.Empty)
                StateChanged?.Invoke(this, PlayerState.Empty);
            State = PlayerState.Empty;
            PlaylistChanged?.Invoke(this, (Playlist, -1));
        }

        public void Start(TimeSpan start_time)
        {
            if (WasapiOutput == null)
                return;

            if (State != PlayerState.Stoped)
                return;

            if (currentTrack == null)
            {
                currentTrack = playlist.First();
                CurrentIndexChanged?.Invoke(this, 0);
            }
            currentTrack.Reader.CurrentTime = start_time;

            if (Provider != null && Provider.ReplaceAudioFile(currentTrack.Reader))
            {
            }
            else
            {
                if (Provider != null)
                {
                    WasapiOutput.Dispose();
                    CreateWasapi();
                }

                Provider = new ReplaceableWaveProvider(currentTrack.Reader);
                if (FrameReported != null)
                {
                    Provider.FrameReported += (o, e) => { FrameReported?.Invoke(this, e); };
                }
                try
                {
                    WasapiOutput.Init(Provider);
                    WasapiOutput.PlaybackStopped += WasapiOutput_PlaybackStopped;
                }
                catch (Exception ex)
                {
                    return;
                }
            }

            WasapiOutput.Play();
            Stopwatch.Start();
            if (State != PlayerState.Playing)
                StateChanged?.Invoke(this, PlayerState.Playing);
            State = PlayerState.Playing;
        }

        private void WasapiOutput_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                if (State == PlayerState.Playing)
                    PlayNext();
            }
        }

        public void Play()
        {
            if (WasapiOutput == null)
                return;

            switch (State)
            {
                case PlayerState.Paused:
                    WasapiOutput.Play();
                    Stopwatch.Start();
                    if (State != PlayerState.Playing)
                        StateChanged?.Invoke(this, PlayerState.Playing);
                    State = PlayerState.Playing;
                    return;
                case PlayerState.Playing:
                    return;
                case PlayerState.Null:
                    return;
                case PlayerState.Empty:
                    return;
                case PlayerState.Stoped:
                    break;
                default:
                    return;
            }
            Start(TimeSpan.Zero);
        }

        public void Stop()
        {
            if (WasapiOutput == null || Provider == null)
                return;
            if (State == PlayerState.Empty || State == PlayerState.Stoped)
                return;

            bool stopped = State != PlayerState.Stoped;
            State = PlayerState.Stoped;
            WasapiOutput.Stop();
            Provider.ResetAudioFile();
            if (stopped)
                StateChanged?.Invoke(this, PlayerState.Stoped);
            Stopwatch.Reset();
            BaseTime = TimeSpan.Zero;
        }

        public void Pause()
        {
            if (WasapiOutput == null)
                return;
            if (State != PlayerState.Playing)
                return;
            bool paused = State != PlayerState.Paused;
            State = PlayerState.Paused;
            WasapiOutput.Pause();
            Stopwatch.Stop();
            if (paused)
                StateChanged?.Invoke(this, PlayerState.Paused);
        }

        public void Seek(TimeSpan time)
        {
            if (currentTrack == null)
                return;
            if (time > (currentTrack as ITrack).Duration)
                return;
            currentTrack.Reader.CurrentTime = time;
            BaseTime = time;
            Stopwatch.Reset();
            if (State == PlayerState.Playing)
            {
                Stopwatch.Start();
            }
        }

        public TimeSpan GetPosition()
        {
            return Stopwatch.Elapsed + BaseTime;
        }

        public void PlayNext()
        {
            if (WasapiOutput == null || State == PlayerState.Empty)
                return;
            Stop();
            int index = playlist.FindIndex(item => item == currentTrack);
            index++;
            if (index >= playlist.Count)
            {
                currentTrack = null;
                CurrentIndexChanged?.Invoke(this, -1);
                return;
            }
            currentTrack = playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }

        public void PlayPrevious()
        {
            if (WasapiOutput == null || State == PlayerState.Empty)
                return;
            Stop();
            int index = playlist.FindIndex(item => item == currentTrack);
            index--;
            if (index < 0)
                return;
            currentTrack = playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }

        public void PlayIndex(int index)
        {
            if (WasapiOutput == null || State == PlayerState.Empty)
                return;
            if (index < 0 || index >= playlist.Count)
                return;
            Stop();
            currentTrack = playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }


        private class ReplaceableWaveProvider : IWaveProvider
        {
            public event EventHandler<(byte[], int)>? FrameReported;

            public ReplaceableWaveProvider(WaveFormat wave_format)
            {
                WaveFormat = new WaveFormat(wave_format.SampleRate, wave_format.BitsPerSample, wave_format.Channels);
            }

            public ReplaceableWaveProvider(WaveStream file)
            {
                WaveReader = file;
                WaveFormat = new WaveFormat(file.WaveFormat.SampleRate, file.WaveFormat.BitsPerSample, file.WaveFormat.Channels);
            }

            public bool ReplaceAudioFile(WaveStream file)
            {
                if (file.WaveFormat.SampleRate != WaveFormat.SampleRate ||
                    file.WaveFormat.Channels != WaveFormat.Channels ||
                    file.WaveFormat.BitsPerSample != WaveFormat.BitsPerSample)
                    return false;
                WaveReader = file;
                return true;
            }
            public void ResetAudioFile()
            {
                WaveReader = null;
            }

            public WaveStream? WaveReader { get; private set; } = null;

            public WaveFormat WaveFormat { get; private set; }


            public int Read(byte[] buffer, int offset, int count)
            {
                if (WaveReader == null)
                    return 0;
                int result = WaveReader.Read(buffer, offset, count);

                if (FrameReported != null)
                {
                    if (report_buffer[current_buffer].Length < result)
                    {
                        report_buffer[current_buffer] = new byte[result];
                    }
                    Array.Copy(buffer, offset, report_buffer[current_buffer], 0, result);
                    FrameReported.Invoke(this, (report_buffer[current_buffer], result));
                    current_buffer = (current_buffer + 1) & 1;
                }

                return result;
            }

            private readonly byte[][] report_buffer = [[], []];
            private int current_buffer = 0;

        }


    }

}
