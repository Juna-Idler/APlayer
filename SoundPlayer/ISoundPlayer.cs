using System;
using System.Collections.Generic;
using Windows.Storage;

namespace APlayer.SoundPlayer
{
    public interface ISoundPlayer
    {
        public string Name { get; }
        public interface IDevice
        {
            string Name { get; }
        }
        public IDevice[] GetDevices();

        public bool Initialize(IDevice? device = null);
        public void Terminalize();

        public bool ChangeDevice(IDevice? device);

        public IDevice? OutputDevice { get; }

        public interface ITrack
        {
            string Name { get; }
            string Path { get; }
            TimeSpan Duration { get; }
        }
        public enum PlayerState { Null, Empty, Stoped, Playing, Paused }


        public event EventHandler<(IReadOnlyList<ITrack> list, int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;
        public event EventHandler<PlayerState>? StateChanged;
        public event EventHandler<(byte[],int)>? FrameReported;

        public uint SampleRate { get; }
        public uint BitsPerSample { get; }
        public uint ChannelCount { get; }

        public PlayerState State { get; }
        public ITrack? CurrentTrack { get; }
        public IReadOnlyList<ITrack> Playlist { get; }
        public void InsertPlaylist(int index, ITrack track);
        public void RemovePlaylist(ITrack track);
        public void RemoveAtPlaylist(int index);

        public int CurrentIndex { get; }

        public double OutputGain { get; set; }

        public void SetPlaylist(IEnumerable<IStorageFile> list, int index = 0);
        public void ResetPlayList();

        public void Start(TimeSpan start_time);
        public void Play();
        public void Stop();
        public void Pause();
        public void Seek(TimeSpan time);
        public TimeSpan GetPosition();

        public void PlayNext();
        public void PlayPrevious();
        public void PlayIndex(int index);
    }

}
