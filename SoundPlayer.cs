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
using System.Reflection;

namespace APlayer
{
    public class SoundPlayer
    {
        public event EventHandler<(IReadOnlyList<AudioFileInputNode> list,int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;

        public AudioGraph? AudioGraph { get; private set; }
        public AudioDeviceOutputNode? DeviceOutputNode { get; private set; }

        public enum PlayerState { Null,Empty, Stoped, Playing, Paused }

        public PlayerState State { get; private set; } = PlayerState.Null;

        public AudioFileInputNode? CurrentInputNode { get; private set; } = null;

        private readonly List<AudioFileInputNode> Playlist = [];

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
            State = PlayerState.Empty;
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
            if (Playlist.Count == 0)
            {
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
            foreach (var item in Playlist)
            {
                item.Dispose();
            }
            Playlist.Clear();

            State = PlayerState.Empty;
            CurrentInputNode = null;
            PlaylistChanged?.Invoke(this, (Playlist, -1));
        }

        public void SetCurrentPlaylistIndex(int index)
        {
            if (Playlist.Count == 0)
                return;
            if (State == PlayerState.Playing || State == PlayerState.Paused)
            {
                Stop();
            }
            index = Math.Clamp(index, 0, Playlist.Count - 1);
            CurrentInputNode = Playlist[index];
            CurrentIndexChanged?.Invoke(this, index);
        }


        public void PlayStandby()
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
            CurrentInputNode.AddOutgoingConnection(DeviceOutputNode);
            CurrentInputNode.FileCompleted += CurrentInputNode_FileCompleted;
            State = PlayerState.Paused;
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
            PlayStandby();
            AudioGraph.Start();
            Stopwatch.Start();
            State = PlayerState.Playing;
        }
        public void Stop()
        {
            if (AudioGraph == null)
                return;
            if (State == PlayerState.Empty)
                return;
            AudioGraph.Stop();
            if (CurrentInputNode != null)
            {
                CurrentInputNode.RemoveOutgoingConnection(DeviceOutputNode);
                CurrentInputNode.FileCompleted -= CurrentInputNode_FileCompleted;
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
            if (CurrentInputNode == null)
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
        public void NextPlay()
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
        public void PreviousPlay()
        {
            if (AudioGraph == null || State == PlayerState.Empty)
                return;
            Stop();
            int index = Playlist.FindIndex(item => item == CurrentInputNode);
            index--;
            if (index < 0)
                return;
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
            NextPlay();
        }
    }
}
