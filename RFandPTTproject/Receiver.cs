using NAudio.Wave;
using System;
using System.Threading.Tasks;

public class Receiver
{
    private RadioChannel channel;
    private IWavePlayer waveOut;
    private BufferedWaveProvider bufferedWaveProvider;

    public Receiver(RadioChannel channel)
    {
        this.channel = channel;
        waveOut = new WaveOutEvent();
        bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        waveOut.Init(bufferedWaveProvider);
    }

    public void Start()
    {
        waveOut.Play();
        Task.Run(ReceiveAudio);
    }

    private async Task ReceiveAudio()
    {
        while (true)
        {
            byte[] audioData = channel.Receive();
            if (audioData != null)
            {
                bufferedWaveProvider.AddSamples(audioData, 0, audioData.Length);
            }
            await Task.Delay(50);
        }
    }

    public void Stop()
    {
        waveOut.Stop();
    }
}