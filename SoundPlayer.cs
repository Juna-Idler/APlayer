using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Diagnostics;

namespace APlayer
{
    public class SoundPlayer
    {
        public event EventHandler<(IReadOnlyList<AudioFileInputNode> list,int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;

        public AudioGraph? AudioGraph { get; private set; }
        public AudioDeviceOutputNode? DeviceOutputNode { get; private set; }

        public enum PlayerState { Null, Stoped, Playing, Paused }

        public PlayerState State { get; private set; } = PlayerState.Null;

        public AudioFileInputNode? NowPlayingInputNode { get; private set; } = null;

        private List<AudioFileInputNode> Playlist = [];
        private int CurrentPlaylistIndex = 0;

        private readonly Stopwatch Stopwatch = new();
        private TimeSpan BaseTime = TimeSpan.Zero;

        public SoundPlayer()
        {
        }

        public async Task Initialize()
        {
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
            State = PlayerState.Stoped;
        }

        public async Task SetPlayList(IEnumerable<IStorageFile> list, int index = 0)
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
                    Playlist.Add(result.FileInputNode);
                }
            }
            CurrentPlaylistIndex = Math.Clamp(index, 0, Playlist.Count - 1);
            PlaylistChanged?.Invoke(this, (Playlist, CurrentPlaylistIndex));
        }
        public void SetCurrentPlaylistIndex(int index)
        {
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            CurrentPlaylistIndex = Math.Clamp(index, 0, Playlist.Count - 1);
            CurrentIndexChanged?.Invoke(this, CurrentPlaylistIndex);
        }


        public void PlayStandby()
        {
            if (State != PlayerState.Stoped)
                return;

            if (AudioGraph == null)
                return;

            if (CurrentPlaylistIndex < 0 || CurrentPlaylistIndex >= Playlist.Count)
                CurrentPlaylistIndex = 0;

            NowPlayingInputNode = Playlist[CurrentPlaylistIndex];
            NowPlayingInputNode.AddOutgoingConnection(DeviceOutputNode);
            NowPlayingInputNode.FileCompleted += NowPlayingInputNode_FileCompleted;
            State = PlayerState.Paused;
        }


        public void Play()
        {
            switch (State)
            {
                case PlayerState.Paused:
                    AudioGraph?.Start();
                    Stopwatch.Start();
                    State = PlayerState.Playing;
                    return;
                case PlayerState.Playing:
                    return;
                case PlayerState.Null:
                    return;
                case PlayerState.Stoped:
                    break;
                default:
                    return;
            }
            PlayStandby();
            AudioGraph?.Start();
            Stopwatch.Start();
            State = PlayerState.Playing;
        }
        public void Stop()
        {
            if (AudioGraph == null)
                return;
            AudioGraph.Stop();
            if (NowPlayingInputNode != null)
            {
                NowPlayingInputNode.RemoveOutgoingConnection(DeviceOutputNode);
                NowPlayingInputNode.FileCompleted -= NowPlayingInputNode_FileCompleted;
                NowPlayingInputNode = null;
            }
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
            State = PlayerState.Paused; 
        }

        public void Seek(TimeSpan time)
        {
            if (NowPlayingInputNode == null)
                return;
            NowPlayingInputNode.Stop();
            NowPlayingInputNode.Seek(time);
            BaseTime = time;
            Stopwatch.Reset();
            if (State == PlayerState.Playing)
            {
                Stopwatch.Start();
            }
            NowPlayingInputNode.Start();
        }
        public void NextPlay()
        {
            Stop();
            CurrentPlaylistIndex++;
            if (CurrentPlaylistIndex >= Playlist.Count)
                return;
            Play();
            CurrentIndexChanged?.Invoke(this, CurrentPlaylistIndex);
        }
        public void PreviousPlay()
        {
            Stop();
            CurrentPlaylistIndex--;
            if (CurrentPlaylistIndex < 0)
                return;
            Play();
            CurrentIndexChanged?.Invoke(this, CurrentPlaylistIndex);
        }


        public TimeSpan GetPosition()
        {
            return Stopwatch.Elapsed + BaseTime;
        }

        public TimeSpan? GetCurrentDuration()
        {
            return NowPlayingInputNode?.Duration;
        }

        private void NowPlayingInputNode_FileCompleted(AudioFileInputNode sender, object args)
        {
            NextPlay();
        }
    }
}
