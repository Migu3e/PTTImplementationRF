using NAudio.Wave;

public class Sender
{
    private WaveInEvent waveIn;
    private byte[] buffer;

    public Sender()
    {
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(48000, 16, 2); // 48kHz, 16-bit, stereo
        waveIn.BufferMilliseconds = 20;
        buffer = new byte[waveIn.WaveFormat.AverageBytesPerSecond / 50];
        waveIn.DataAvailable += WaveIn_DataAvailable;
    }

    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        Buffer.BlockCopy(e.Buffer, 0, buffer, 0, Math.Min(e.BytesRecorded, buffer.Length));
    }

    public void Start()
    {
        waveIn.StartRecording();
    }

    public void Stop()
    {
        waveIn.StopRecording();
    }

    public int ReadAudio(byte[] buffer, int offset, int count)
    {
        int bytesToCopy = Math.Min(count, this.buffer.Length);
        Buffer.BlockCopy(this.buffer, 0, buffer, offset, bytesToCopy);
        return bytesToCopy;
    }
}