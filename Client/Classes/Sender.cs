using NAudio.Wave;

public class Sender
{
    private WaveInEvent waveIn;
    private List<byte> buffer = new List<byte>();
    public const int CHUNK_SIZE = 16384; // Increased chunk size

    public Sender()
    {
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(44100, 16, 1); // 44.1kHz, 16-bit, mono
        waveIn.BufferMilliseconds = 1000; // Increased buffer size
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