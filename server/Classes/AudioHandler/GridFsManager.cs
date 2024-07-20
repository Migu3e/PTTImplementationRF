using server.Const;
using server.Interface;

namespace server.Classes.AudioHandler;

public class GridFsManager : IGridFsManager
{
    private readonly string audioDirectory;

    public GridFsManager(string baseDirectory)
    {
        audioDirectory = Path.Combine(baseDirectory, Constants.FolderToSave);
        Directory.CreateDirectory(audioDirectory);
    }

    public async Task SaveAudioAsync(string filename, byte[] audioData)
    {
        string filePath = Path.Combine(audioDirectory, filename);
        await File.WriteAllBytesAsync(filePath, audioData);
    }

    public async Task<byte[]> GetAudioAsync(string filename)
    {
        string filePath = Path.Combine(audioDirectory, filename);
        if (File.Exists(filePath))
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        throw new FileNotFoundException($"Audio file not found: {filename}");
    }
}