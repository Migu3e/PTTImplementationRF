using Client.Interfaces;

namespace Client.Classes;

public class RadioChannel : IRadioChannel
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