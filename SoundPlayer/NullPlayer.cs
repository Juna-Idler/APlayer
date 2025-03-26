using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static APlayer.SoundPlayer.ISoundPlayer;
using Windows.Storage;

namespace APlayer.SoundPlayer
{
    public class NullPlayer : ISoundPlayer
    {
        public string Name { get => "Null Player"; }

        public IDevice[] GetDevices() { return []; }

        public bool Initialize(IDevice? device = null) { return true; }
        public void Terminalize() { }

        public IDevice? OutputDevice { get => null; }

        public event EventHandler<(IReadOnlyList<ITrack> list, int index)>? PlaylistChanged;
        public event EventHandler<int>? CurrentIndexChanged;
        public event EventHandler<PlayerState>? StateChanged;
        public event EventHandler<(byte[], int)>? FrameReported;

        public uint SampleRate { get => 0; }
        public uint BitsPerSample { get => 0; }
        public uint ChannelCount { get => 0; }

        public PlayerState State { get => PlayerState.Null; }
        public ITrack? CurrentTrack { get => null; }
        public IReadOnlyList<ITrack> Playlist { get => []; }
        public void InsertPlaylist(int index, ITrack track) { }
        public void RemovePlaylist(ITrack track) { }
        public void RemoveAtPlaylist(int index) { }

        public int CurrentIndex { get => -1; }

        public double OutputGain { get; set; }

        public void SetPlaylist(IEnumerable<IStorageFile> list, int index = 0) { }
        public void ResetPlayList() { }

        public void Start(TimeSpan start_time) { }
        public void Play() { }
        public void Stop() { }
        public void Pause() { }
        public void Seek(TimeSpan time) { }
        public TimeSpan GetPosition() { return TimeSpan.Zero; }

        public void PlayNext() { }
        public void PlayPrevious() { }
        public void PlayIndex(int index) { }

    }
}
