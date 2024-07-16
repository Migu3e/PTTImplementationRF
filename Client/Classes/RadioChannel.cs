using NAudio.Wave;

public class RadioChannel
{
    private byte[] currentAudioBuffer;

    public void Transmit(byte[] audioData)
    {
        currentAudioBuffer = audioData;
    }

    public byte[] Receive()
    {
        return currentAudioBuffer;
    }
}