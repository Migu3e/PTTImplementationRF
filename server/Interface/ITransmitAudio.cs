using server.Classes;
using server.Classes.ClientHandler;

namespace server.Interface;

public interface ITransmitAudio
{
    Task BroadcastAudioAsync(Client sender, byte[] audioData, int length);
    Task SendAudioToClientAsync(Client client, byte[] audioData, int length);
}