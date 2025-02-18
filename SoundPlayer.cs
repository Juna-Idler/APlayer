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

namespace APlayer
{
    public class SoundPlayer
    {
        public event EventHandler<(IReadOnlyList<AudioFileInputNode> list, int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;
        public event EventHandler<PlayerState>? StateChanged;
        public event EventHandler<(float left,float right)>? PeakReported;

        public AudioGraph? AudioGraph { get; private set; }
        public AudioDeviceOutputNode? DeviceOutputNode { get; private set; }

        public enum PlayerState { Null, Empty, Stoped, Playing, Paused }

        public PlayerState State { get; private set; } = PlayerState.Null;

        public AudioFileInputNode? CurrentInputNode { get; private set; } = null;

        public List<AudioFileInputNode> Playlist { get; private set; } = [];

        public int CurrentIndex
        {
            get => Playlist.FindIndex(item => item == CurrentInputNode);
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

        public async Task Initialize()
        {
            if (AudioGraph != null)
                return;

            AudioGraphSettings settings = new(Windows.Media.Render.AudioRenderCategory.Media);
            {
                var result = await AudioGraph.CreateAsync(settings);
                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    throw new Exception();
                }

                AudioGraph = result.Graph;
            }
            {
                var result = await AudioGraph.CreateDeviceOutputNodeAsync();
                if (result.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    throw new Exception();
                }
                DeviceOutputNode = result.DeviceOutputNode;
            }
            State = PlayerState.Empty;
        }

        public async Task SetPlaylist(IEnumerable<IStorageFile> list, int index = 0)
        {
            if (AudioGraph == null)
                return;
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            foreach (var item in Playlist)
            {
                item.Dispose();
            }
            Playlist.Clear();
            foreach (var item in list)
            {
                var result = await AudioGraph.CreateFileInputNodeAsync(item);
                if (result.Status == AudioFileNodeCreationStatus.Success)
                {
                    result.FileInputNode.EndTime = result.FileInputNode.Duration;
                    Playlist.Add(result.FileInputNode);
                }
            }
            if (Playlist.Count == 0)
            {
                if (State != PlayerState.Empty)
                    StateChanged?.Invoke(this, PlayerState.Empty);
                State = PlayerState.Empty;
                CurrentInputNode = null;
                PlaylistChanged?.Invoke(this, (Playlist, -1));
            }
            else
            {
                State = PlayerState.Stoped;
                int i = Math.Clamp(index, 0, Playlist.Count - 1);
                CurrentInputNode = Playlist[i];
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
            CurrentInputNode = null;
            foreach (var item in Playlist)
            {
                item.Dispose();
            }
            Playlist.Clear();

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

            if (CurrentInputNode == null)
            {
                CurrentInputNode = Playlist.First();
                CurrentIndexChanged?.Invoke(this, 0);
            }
            CurrentInputNode.Seek(start_time);
            CurrentInputNode.AddOutgoingConnection(DeviceOutputNode);
            if (FrameOutputNode != null)
                CurrentInputNode.AddOutgoingConnection(FrameOutputNode);
            CurrentInputNode.FileCompleted += CurrentInputNode_FileCompleted;
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
            if (CurrentInputNode != null)
            {
                CurrentInputNode.RemoveOutgoingConnection(DeviceOutputNode);
                if (FrameOutputNode != null)
                    CurrentInputNode.RemoveOutgoingConnection(FrameOutputNode);
                CurrentInputNode.FileCompleted -= CurrentInputNode_FileCompleted;
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
            if (CurrentInputNode == null)
                return;
            if (time > CurrentInputNode.Duration)
                return;
            CurrentInputNode.Stop();
            CurrentInputNode.Seek(time);
            BaseTime = time;
            Stopwatch.Reset();
            if (State == PlayerState.Playing)
            {
                Stopwatch.Start();
            }
            CurrentInputNode.Start();
        }
        public void PlayNext()
        {
            if (AudioGraph == null || State == PlayerState.Empty)
                return;
            Stop();
            int index = Playlist.FindIndex(item => item == CurrentInputNode);
            index++;
            if (index >= Playlist.Count)
            {
                CurrentInputNode = null;
                CurrentIndexChanged?.Invoke(this, -1);
                return;
            }
            CurrentInputNode = Playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }
        public void PlayPrevious()
        {
            if (AudioGraph == null || State == PlayerState.Empty)
                return;
            Stop();
            int index = Playlist.FindIndex(item => item == CurrentInputNode);
            index--;
            if (index < 0)
                return;
            CurrentInputNode = Playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }

        public void PlayIndex(int index)
        {
            if (AudioGraph == null || State == PlayerState.Empty)
                return;
            if (index < 0 || index >= Playlist.Count)
                return;
            Stop();
            CurrentInputNode = Playlist[index];
            Play();
            CurrentIndexChanged?.Invoke(this, index);
        }

        public TimeSpan GetPosition()
        {
            return Stopwatch.Elapsed + BaseTime;
        }

        public TimeSpan? GetCurrentDuration()
        {
            return CurrentInputNode?.Duration;
        }

        private void CurrentInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            PlayNext();
        }

        public void InsertPeakDetector()
        {
            if (AudioGraph == null) return;
            if (FrameOutputNode != null)
                return;

            FrameOutputNode = AudioGraph.CreateFrameOutputNode();
            if (CurrentInputNode != null)
                CurrentInputNode.AddOutgoingConnection(FrameOutputNode);
            AudioGraph.QuantumStarted += AudioGraph_QuantumStarted;
        }
        public void RemovePeakDetector()
        {
            if (AudioGraph == null) return;
            if (FrameOutputNode ==  null) return;

            AudioGraph.QuantumStarted -= AudioGraph_QuantumStarted;
            if (CurrentInputNode != null)
                CurrentInputNode.RemoveOutgoingConnection(FrameOutputNode);
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
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;
                uint samples = buffer.Length / (sizeof(float) / sizeof(byte));
                float left_peak = 0;
                float right_peak = 0;
                if (FrameOutputNode.EncodingProperties.ChannelCount == 2)
                {
                    for (int i = 0; i < samples; i += 2)
                    {
                        left_peak = Math.Max(left_peak, dataInFloat[i]);
                        right_peak = Math.Max(right_peak, dataInFloat[i + 1]);
                    }
                }
                else
                {
                    for (int i = 0; i < samples; i++)
                    {
                        left_peak = Math.Max(left_peak, dataInFloat[i]);
                    }
                }
                PeakReported?.Invoke(this, (left_peak, right_peak));
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
