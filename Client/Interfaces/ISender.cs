using System.Net.Sockets;

namespace Client.Interfaces;

public interface ISender
{
    void Start();
    void Stop();
    int ReadAudio(byte[] outputBuffer, int offset, int count);
    bool IsDataAvailable();
    Task SendFullAudioToServer(NetworkStream stream, IFullAudioMaker fullAudioMaker);
    Task TransmitAudioToServer(NetworkStream stream, ISender sender, byte channel);
}