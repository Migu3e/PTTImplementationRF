using server.Const;
using server.Interface;
using System.Text;

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
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            // Write WAV header
            await WriteWavHeaderAsync(fileStream, audioData.Length);
            // Write audio data
            await fileStream.WriteAsync(audioData, 0, audioData.Length);
        }
    }

    private async Task WriteWavHeaderAsync(Stream stream, int dataSize)
    {
        // RIFF header
        await WriteStringAsync(stream, "RIFF");
        await WriteInt32Async(stream, dataSize + 36); // File size - 8
        await WriteStringAsync(stream, "WAVE");

        // Format chunk
        await WriteStringAsync(stream, "fmt ");
        await WriteInt32Async(stream, 16); // Subchunk1Size
        await WriteInt16Async(stream, 1); // AudioFormat (PCM)
        await WriteInt16Async(stream, 1); // NumChannels (Mono)
        await WriteInt32Async(stream, 44100); // SampleRate
        await WriteInt32Async(stream, 44100 * 2); // ByteRate
        await WriteInt16Async(stream, 2); // BlockAlign
        await WriteInt16Async(stream, 16); // BitsPerSample

        // Data chunk
        await WriteStringAsync(stream, "data");
        await WriteInt32Async(stream, dataSize);
    }

    private async Task WriteStringAsync(Stream stream, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private async Task WriteInt32Async(Stream stream, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }

    private async Task WriteInt16Async(Stream stream, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        await stream.WriteAsync(bytes, 0, bytes.Length);
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