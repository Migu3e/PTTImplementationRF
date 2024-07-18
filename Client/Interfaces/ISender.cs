namespace Client.Interfaces;

public interface ISender
{
    void Start();
    void Stop();
    int ReadAudio(byte[] outputBuffer, int offset, int count);
    bool IsDataAvailable();
}