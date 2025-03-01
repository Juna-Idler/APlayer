using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Media;
using System.Runtime.InteropServices.Marshalling;
using static APlayer.ISoundPlayer;
using Windows.Media.Playlists;
using Windows.Devices.Enumeration;

namespace APlayer
{

    public class SoundPlayer : ISoundPlayer
    {
        public event EventHandler<(IReadOnlyList<ITrack> list, int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;
        public event EventHandler<PlayerState>? StateChanged;
        public event EventHandler<float[]>? FrameReported;

        public class Device(DeviceInformation device) : IDevice
        {
            public string Name { get; set; } = device.Name;
            public DeviceInformation DeviceInformation { get; set; } = device;
        }
        public async Task<IReadOnlyList<IDevice>> GetDevices()
        {
            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Media.Devices.MediaDevice.GetAudioRenderSelector());
            List<Device> result = new(devices.Select(d => new Device(d)));
            return result;
        }


        private class Track(AudioFileInputNode fileInputNode) : ITrack
        {
            public AudioFileInputNode FileInputNode { get; set; } = fileInputNode;

            public string Name { get => FileInputNode.SourceFile.Name; }
            public string Path { get => FileInputNode.SourceFile.Path; }

            public TimeSpan Duration { get => FileInputNode.Duration; }
        }

        public IDevice? OutputDevice { get; private set; }

        private AudioGraph? AudioGraph { get; set; }
        private AudioDeviceOutputNode? DeviceOutputNode { get; set; }

        public uint ChannelCount => (AudioGraph == null) ? 0 : AudioGraph.EncodingProperties.ChannelCount;


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
                if (DeviceOutputNode == null)
                    return 0;
                else return DeviceOutputNode.OutgoingGain;
            }
            set
            {
                if (DeviceOutputNode != null)
                    DeviceOutputNode.OutgoingGain = value;
            }
        }
            

        private readonly Stopwatch Stopwatch = new();
        private TimeSpan BaseTime = TimeSpan.Zero;

        private AudioFrameOutputNode? FrameOutputNode = null;


        public SoundPlayer()
        {
        }

        public async Task<bool> Initialize(IDevice? device = null)
        {
            Terminalize();

            OutputDevice = device;

            AudioGraphSettings settings = new(Windows.Media.Render.AudioRenderCategory.Media)
            {
                PrimaryRenderDevice = (device as Device)?.DeviceInformation,
                MaxPlaybackSpeedFactor = 1
            };
            //            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            //            settings.DesiredSamplesPerQuantum = 882;
            //            settings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw;
            {
                var result = await AudioGraph.CreateAsync(settings);
                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    return false;
                }

                AudioGraph = result.Graph;
                AudioGraph.UnrecoverableErrorOccurred += AudioGraph_UnrecoverableErrorOccurred;
            }
            {
                var result = await AudioGraph.CreateDeviceOutputNodeAsync();
                if (result.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    Terminalize();
                    return false;
                }
                DeviceOutputNode = result.DeviceOutputNode;
            }
            State = PlayerState.Empty;
            return true;
        }

        private void AudioGraph_UnrecoverableErrorOccurred(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            if (sender == AudioGraph && args.Error != AudioGraphUnrecoverableError.None)
            {
                Terminalize();
            }
        }

        public void Terminalize()
        {
            ResetPlayList();
            FrameOutputNode?.Dispose();
            FrameOutputNode = null;
            DeviceOutputNode?.Dispose();
            DeviceOutputNode = null;
            AudioGraph?.Dispose();
            AudioGraph = null;
            OutputDevice = null;
        }

        public async Task SetPlaylist(IEnumerable<IStorageFile> list, int index = 0)
        {
            if (AudioGraph == null)
                return;
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            foreach (var item in playlist)
            {
                item.FileInputNode.Dispose();
            }
            playlist.Clear();
            foreach (var item in list)
            {
                var result = await AudioGraph.CreateFileInputNodeAsync(item);
                if (result.Status == AudioFileNodeCreationStatus.Success)
                {
                    result.FileInputNode.EndTime = result.FileInputNode.Duration;
                   playlist.Add(new (result.FileInputNode));
                }
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
            if (AudioGraph == null)
                return;
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            currentTrack = null;
            foreach (var item in playlist)
            {
                item.FileInputNode.Dispose();
            }
            playlist.Clear();

            if (State != PlayerState.Empty)
                StateChanged?.Invoke(this, PlayerState.Empty);
            State = PlayerState.Empty;
            PlaylistChanged?.Invoke(this, (Playlist, -1));
        }


        public void Start(TimeSpan start_time)
        {
            if (AudioGraph == null)
                return;

            if (State != PlayerState.Stoped)
                return;

            if (currentTrack == null)
            {
                currentTrack = playlist.First();
                CurrentIndexChanged?.Invoke(this, 0);
            }
            currentTrack.FileInputNode.Seek(start_time);
            currentTrack.FileInputNode.AddOutgoingConnection(DeviceOutputNode);
            if (FrameOutputNode != null)
                currentTrack.FileInputNode.AddOutgoingConnection(FrameOutputNode);
            currentTrack.FileInputNode.FileCompleted += CurrentInputNode_FileCompleted;
            AudioGraph.Start();
            Stopwatch.Start();
            if (State != PlayerState.Playing)
                StateChanged?.Invoke(this, PlayerState.Playing);
            State = PlayerState.Playing;
        }

        public void Play()
        {
            if (AudioGraph == null)
                return;

            switch (State)
            {
                case PlayerState.Paused:
                    AudioGraph.Start();
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
            if (AudioGraph == null)
                return;
            if (State == PlayerState.Empty || State == PlayerState.Stoped)
                return;
            AudioGraph.Stop();
            if (currentTrack != null)
            {
                currentTrack.FileInputNode.RemoveOutgoingConnection(DeviceOutputNode);
                if (FrameOutputNode != null)
                    currentTrack.FileInputNode.RemoveOutgoingConnection(FrameOutputNode);
                currentTrack.FileInputNode.FileCompleted -= CurrentInputNode_FileCompleted;
            }
            if (State != PlayerState.Stoped)
                StateChanged?.Invoke(this, PlayerState.Stoped);
            State = PlayerState.Stoped;
            Stopwatch.Reset();
            BaseTime = TimeSpan.Zero;
        }
        public void Pause()
        {
            if (AudioGraph == null)
                return;
            if (State != PlayerState.Playing)
                return;
            AudioGraph.Stop();
            Stopwatch.Stop();
            if (State != PlayerState.Paused)
                StateChanged?.Invoke(this, PlayerState.Paused);
            State = PlayerState.Paused; 
        }

        public void Seek(TimeSpan time)
        {
            if (currentTrack == null)
                return;
            if (time > (currentTrack as ITrack).Duration)
                return;
            currentTrack.FileInputNode.Stop();
            currentTrack.FileInputNode.Seek(time);
            BaseTime = time;
            Stopwatch.Reset();
            if (State == PlayerState.Playing)
            {
                Stopwatch.Start();
            }
            currentTrack.FileInputNode.Start();
        }
        public void PlayNext()
        {
            if (AudioGraph == null || State == PlayerState.Empty)
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
            if (AudioGraph == null || State == PlayerState.Empty)
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
            if (AudioGraph == null || State == PlayerState.Empty)
                return;
            if (index < 0 || index >= playlist.Count)
                return;
            Stop();
            currentTrack = playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }

        public TimeSpan GetPosition()
        {
            return Stopwatch.Elapsed + BaseTime;
        }

        private void CurrentInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            PlayNext();
        }

        private float[] frame_buffer = [];
        public void InsertPeakDetector()
        {
            if (AudioGraph == null) return;
            if (FrameOutputNode != null)
                return;

            frame_buffer = new float[AudioGraph.SamplesPerQuantum * AudioGraph.EncodingProperties.ChannelCount];

            FrameOutputNode = AudioGraph.CreateFrameOutputNode();
            if (currentTrack != null)
                currentTrack.FileInputNode.AddOutgoingConnection(FrameOutputNode);
            AudioGraph.QuantumStarted += AudioGraph_QuantumStarted;
        }
        public void RemovePeakDetector()
        {
            if (AudioGraph == null) return;
            if (FrameOutputNode ==  null) return;

            AudioGraph.QuantumStarted -= AudioGraph_QuantumStarted;
            if (currentTrack != null)
                currentTrack.FileInputNode.RemoveOutgoingConnection(FrameOutputNode);
            FrameOutputNode.Dispose();
            FrameOutputNode = null;
        }

        unsafe private void AudioGraph_QuantumStarted(AudioGraph sender, object args)
        {
            if (FrameOutputNode == null)
                return;
            var frame = FrameOutputNode.GetFrame();
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                System.Runtime.InteropServices.Marshal.Copy((nint)dataInBytes, frame_buffer, 0, (int)(buffer.Length / (sizeof(float) / sizeof(byte))));
                FrameReported?.Invoke(this, frame_buffer);
            }
        }


    }


    [GeneratedComInterface]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe partial interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

}
