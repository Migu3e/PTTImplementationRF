using System.Net.Sockets;
using Client.Const;
using Client.Interfaces;
using NAudio.Wave;

namespace Client.Classes.AudioHandler;

public class Sender : ISender
{
    private WaveInEvent waveIn;
    private List<byte> buffer = new List<byte>();
    public const int CHUNK_SIZE = 16384;

    public Sender()
    {
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
        waveIn.BufferMilliseconds = 10;
        waveIn.DataAvailable += WaveIn_DataAvailable;
    }

    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        lock (buffer)
        {
            buffer.AddRange(e.Buffer.Take(e.BytesRecorded));
        }
    }

    public void Start()
    {
        waveIn.StartRecording();
    }

    public void Stop()
    {
        waveIn.StopRecording();
    }

    public int ReadAudio(byte[] outputBuffer, int offset, int count)
    {
        lock (buffer)
        {
            int bytesToCopy = Math.Min(count, buffer.Count);
            buffer.CopyTo(0, outputBuffer, offset, bytesToCopy);
            buffer.RemoveRange(0, bytesToCopy);
            return bytesToCopy;
        }
    }

    public bool IsDataAvailable()
    {
        return buffer.Count >= CHUNK_SIZE;
    }
    public async Task TransmitAudioToServer(NetworkStream stream, ISender sender)
    {
        byte[] buffer = new byte[CHUNK_SIZE];
        if (sender.IsDataAvailable())
        {
            int bytesRead = sender.ReadAudio(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
                await stream.WriteAsync(header, 0, header.Length);

                byte[] lengthBytes = BitConverter.GetBytes(bytesRead);
                await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
    public async Task SendFullAudioToServer(NetworkStream stream, IFullAudioMaker fullAudioMaker)
    {
        byte[] fullAudio = fullAudioMaker.GetFullAudioData();
        if (fullAudio.Length == 0)
        {
            Console.WriteLine(Constants.NoAudioDataMessage);
            return;
        }

        byte[] header = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        await stream.WriteAsync(header, 0, header.Length);

        byte[] lengthBytes = BitConverter.GetBytes(fullAudio.Length);
        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

        await stream.WriteAsync(fullAudio, 0, fullAudio.Length);

        Console.WriteLine($"{Constants.SendAudioToServer} ({fullAudio.Length} bytes).");
    }

}
