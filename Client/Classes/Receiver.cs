using NAudio.Wave;

public class Receiver
{
    private IWavePlayer waveOut;
    private BufferedWaveProvider bufferedWaveProvider;

    public Receiver()
    {
        waveOut = new WaveOutEvent();
        bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 2)); // Match Sender's format
        bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(0.5); // Reduce latency
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
    }
}