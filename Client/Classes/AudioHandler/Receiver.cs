using System.Net.Sockets;
using Client.Const;
using Client.Interfaces;
using NAudio.Wave;

namespace Client.Classes.AudioHandler;

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
    public async Task ReceiveAudioFromServer(NetworkStream stream, IReceiver receiver)
    {
        byte[] buffer = new byte[4096];
        while (true)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    receiver.PlayAudio(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Constants.ErrorMessage} {ex.Message}");
                break;
            }
        }
    }
}
