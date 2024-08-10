using System.Text;
using server.Const;
using server.Interface;

namespace GridFs;

public class GridFsManager : IGridFsManager
{
    private readonly string audioDirectory;

    public GridFsManager(string baseDirectory)
    {
        audioDirectory = Path.Combine(baseDirectory, Constants.FolderToSave);
        Directory.CreateDirectory(audioDirectory);
    }

    public async Task SaveAudioAsync(string filename, byte[] audioData,bool clienttype)
    {
        if (clienttype)
        {
            string filePath = Path.Combine(audioDirectory, filename);
            Console.WriteLine($"Saving audio file: {filename}, Data size: {audioData.Length} bytes");

            await File.WriteAllBytesAsync(filePath, audioData);

            Console.WriteLine($"Audio file saved: {filePath}");
        }
        else
        {
            string filePath = Path.Combine(audioDirectory, filename);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                // Log audio data length before writing
                Console.WriteLine($"Saving audio file: {filename}, Data size: {audioData.Length} bytes");

                // Write WAV header
                await WriteWavHeaderAsync(fileStream, audioData.Length);

                // Write audio data
                await fileStream.WriteAsync(audioData, 0, audioData.Length);
            }
        }

    }

    private async Task WriteWavHeaderAsync(Stream stream, int dataSize)
    {
        // RIFF header
        await WriteStringAsync(stream, "RIFF");
        await WriteInt32Async(stream, dataSize + 36); // File size minus 8 bytes
        await WriteStringAsync(stream, "WAVE");

        // Format chunk
        await WriteStringAsync(stream, "fmt ");
        await WriteInt32Async(stream, 16); // Subchunk1Size (16 for PCM)
        await WriteInt16Async(stream, 1); // AudioFormat (1 for PCM)
        await WriteInt16Async(stream, 1); // NumChannels (1 for mono)
        await WriteInt32Async(stream, 44100); // SampleRate (44100 Hz)
        await WriteInt32Async(stream, 44100 * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        await WriteInt16Async(stream, 2); // BlockAlign (NumChannels * BitsPerSample/8)
        await WriteInt16Async(stream, 16); // BitsPerSample (16 bits per sample)

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
}
