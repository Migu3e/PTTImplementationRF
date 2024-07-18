using Client.Interfaces;
using NAudio.Wave;

namespace Client.Classes;

public class Sender : ISender
{
    private WaveInEvent waveIn;
    private List<byte> buffer = new List<byte>();
    public const int CHUNK_SIZE = 16384;

    public Sender()
    {
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
        waveIn.BufferMilliseconds = 1000;
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
}
