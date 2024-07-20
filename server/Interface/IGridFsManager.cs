namespace server.Interface;

public interface IGridFsManager
{
    Task SaveAudioAsync(string filename, byte[] audioData);
}