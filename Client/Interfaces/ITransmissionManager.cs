using System.Net.Sockets;

namespace Client.Interfaces;

public interface ITransmissionManager
{
    void StartTransmission();
    Task StopTransmission(NetworkStream stream);
    Task ToggleTransmission(NetworkStream stream);
    Task TransmitAudio(NetworkStream stream, byte currentChannel);
}