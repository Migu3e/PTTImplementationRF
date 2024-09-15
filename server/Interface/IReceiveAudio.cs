using System.Net.Sockets;
using server.Classes;
using server.Classes.ClientHandler;

namespace server.Interface;

public interface IReceiveAudio
{
    Task HandleFullAudioTransmissionAsyncWebSockets(Client client, byte[] audioData);
    Task HandleRealtimeAudioAsyncWebSockets(Client sender, byte[] audioData);
}