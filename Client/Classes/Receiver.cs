using Client.Interfaces;
using NAudio.Wave;

namespace Client.Classes;

public class Receiver : IReceiver
{
    private IWavePlayer waveOut;
    private BufferedWaveProvider bufferedWaveProvider;

    public Receiver()
    {
        waveOut = new WaveOutEvent();
        bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(1000);
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
