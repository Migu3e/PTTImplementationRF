using NAudio.Wave;
using System;
using System.IO;
using Client.Const;
using Client.Interfaces;
using System.Collections.Generic;

namespace Client.Classes
{
    public class FullAudioMaker : IFullAudioMaker
    {
        private WaveInEvent waveSource = null;
        private List<byte> audioBuffer = new List<byte>();

        public void StartRecording()
        {
            try
            {
                waveSource = new WaveInEvent();
                waveSource.WaveFormat = new WaveFormat(44100, 16, 1);
                waveSource.DataAvailable += OnDataAvailable;
                audioBuffer.Clear();
                waveSource.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Constants.ErrorMessage} {ex.Message}");
                StopRecording();
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            audioBuffer.AddRange(new ReadOnlySpan<byte>(e.Buffer, 0, e.BytesRecorded).ToArray());
        }

        public void StopRecording()
        {
            waveSource?.StopRecording();
            waveSource?.Dispose();
            waveSource = null;
        }

        public byte[] GetFullAudioData()
        {
            if (audioBuffer.Count > 0)
            {
                return audioBuffer.ToArray();
            }
            else
            {
                Console.WriteLine(Constants.NoAudioDataMessage);
                return new byte[0];
            }
        }
    }
}