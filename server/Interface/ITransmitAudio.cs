using server.Classes;

namespace server.Interface;

public interface ITransmitAudio
{
    Task BroadcastAudioAsync(string senderId, byte[] audioData, int length);
    Task SendAudioToClientAsync(Client client, byte[] audioData, int length);
}