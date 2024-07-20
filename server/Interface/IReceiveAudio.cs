using System.Net.Sockets;
using server.Classes;
using server.Classes.ClientHandler;

namespace server.Interface;

public interface IReceiveAudio
{
    Task HandleRealtimeAudioAsync(Client sender, NetworkStream stream);
    Task HandleFullAudioTransmissionAsync(Client client, NetworkStream stream);
}