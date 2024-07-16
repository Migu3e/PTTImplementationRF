using NAudio.Wave;
using System;

namespace Client.Classes
{
    public class Receiver
    {
        private IWavePlayer waveOut;
        private BufferedWaveProvider bufferedWaveProvider;

        public Receiver()
        {
            waveOut = new WaveOutEvent();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 1)); // Mono, 16-bit, 44.1kHz
            bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(1000); // Adjust buffer duration
            bufferedWaveProvider.DiscardOnBufferOverflow = true;
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();
        }

        public void PlayAudio(byte[] buffer, int offset, int count)
        {
            bufferedWaveProvider.AddSamples(buffer, offset, count);
        }

        public void Stop()
        {
            waveOut.Stop();
            waveOut.Dispose();
        }
    }
}