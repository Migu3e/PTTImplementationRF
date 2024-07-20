namespace server.Interface;

public interface IGridFsManager
{
    Task SaveAudioAsync(string filename, byte[] audioData);
    Task<byte[]> GetAudioAsync(string filename);
}