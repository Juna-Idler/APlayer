using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static APlayer.SoundPlayer.ISoundPlayer;

namespace APlayer.SoundPlayer
{
    public class LoopbackCapture
    {
        public event EventHandler<(double left,double right)>? OnPeakChanged;

        public bool IsCapturing => WasapiLoopbackCapture != null;

        private WasapiLoopbackCapture? WasapiLoopbackCapture = null;

        private bool Stereo = false;

        public void Start(string? device_name = null)
        {
            MMDevice? device = null;
            if (device_name != null)
            {
                MMDeviceEnumerator enumerator = new();
                var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                device = endpoints.FirstOrDefault(d => d.FriendlyName == device_name);
            }
            WasapiLoopbackCapture = device == null ? new WasapiLoopbackCapture() : new WasapiLoopbackCapture(device);

            WasapiLoopbackCapture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(WasapiLoopbackCapture.WaveFormat.SampleRate, WasapiLoopbackCapture.WaveFormat.Channels);
            Stereo = WasapiLoopbackCapture.WaveFormat.Channels == 2;
            WasapiLoopbackCapture.DataAvailable += WasapiLoopbackCapture_DataAvailable;
            WasapiLoopbackCapture.RecordingStopped += WasapiLoopbackCapture_RecordingStopped;
            WasapiLoopbackCapture.StartRecording();
        }

        public void Stop()
        {
            if (WasapiLoopbackCapture == null)
                return;
            WasapiLoopbackCapture.DataAvailable -= WasapiLoopbackCapture_DataAvailable;
            WasapiLoopbackCapture.RecordingStopped -= WasapiLoopbackCapture_RecordingStopped;
            WasapiLoopbackCapture.StopRecording();
            WasapiLoopbackCapture.Dispose();
            WasapiLoopbackCapture = null;

            OnPeakChanged?.Invoke(this, (0,0));
        }

        private void WasapiLoopbackCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (Stereo)
            {
                float lmin = 0, lmax = 0;
                float rmin = 0, rmax = 0;
                for (int i = 0; i < e.BytesRecorded; i += 8)
                {
                    float f = BitConverter.ToSingle(e.Buffer, i);
                    lmin = Math.Min(lmin, f);
                    lmax = Math.Max(lmax, f);
                    f = BitConverter.ToSingle(e.Buffer, i + 4);
                    rmin = Math.Min(rmin, f);
                    rmax = Math.Max(rmax, f);
                }
                OnPeakChanged?.Invoke(this, ((lmax - lmin) / 2, (rmax - rmin) / 2));

            }
            else
            {
                float min = 0, max = 0;
                for (int i = 0; i < e.BytesRecorded; i += 4)
                {
                    float f = BitConverter.ToSingle(e.Buffer, i);
                    min = Math.Min(min, f);
                    max = Math.Max(max, f);
                }
                float peak = (max - min) / 2;
                OnPeakChanged?.Invoke(this, (peak,peak));
            }
        }
        private void WasapiLoopbackCapture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            OnPeakChanged?.Invoke(this, (0,0));
        }

    }
}
