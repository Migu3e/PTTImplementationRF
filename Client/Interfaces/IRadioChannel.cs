namespace Client.Interfaces;

public interface IRadioChannel
{
    void Transmit(byte[] audioData);
    byte[] Receive();
}