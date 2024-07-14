using NAudio.Wave;

public class Sender
{
    private RadioChannel channel;
    private WaveInEvent waveIn;

    public Sender(RadioChannel channel)
    {
        this.channel = channel;
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
        waveIn.BufferMilliseconds = 50;
        waveIn.DataAvailable += WaveIn_DataAvailable;
    }

    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        channel.Transmit(e.Buffer);
    }

    public void Start()
    {
        waveIn.StartRecording();
    }

    public void Stop()
    {
        waveIn.StopRecording();
    }
}