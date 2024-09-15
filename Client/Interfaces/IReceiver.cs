using System.Net.Sockets;

namespace Client.Interfaces;

public interface IReceiver
{
    void PlayAudio(byte[] buffer, int offset, int count);
    void Stop();
    Task ReceiveAudioFromServer(NetworkStream stream, IReceiver receiver);
}