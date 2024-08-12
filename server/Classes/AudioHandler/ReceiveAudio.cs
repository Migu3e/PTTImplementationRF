using System.Net.Sockets;
using server.Classes.ClientHandler;
using server.Const;
using server.Interface;

namespace server.Classes.AudioHandler;

public class ReceiveAudio : IReceiveAudio
{
    private readonly ITransmitAudio _transmitAudio;
    private readonly IGridFsManager _gridFsManager;

    public ReceiveAudio(ITransmitAudio transmitAudio, IGridFsManager gridFsManager)
    {
        _transmitAudio = transmitAudio;
        _gridFsManager = gridFsManager;
    }

    public async Task HandleRealtimeAudioAsync(Client sender, NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadAsync(lengthBuffer, 0, 4);
        int audioLength = BitConverter.ToInt32(lengthBuffer, 0);

        byte[] audioBuffer = new byte[audioLength];
        int bytesRead = await stream.ReadAsync(audioBuffer, 0, audioLength);
        if (bytesRead == audioLength)
        {
            await _transmitAudio.BroadcastAudioAsync(sender, audioBuffer, bytesRead);
        }
    }
    public async Task HandleRealtimeAudioAsyncWebSockets(Client sender, byte[] audioData)
    {
        Console.WriteLine($"websocket {audioData}");
        await _transmitAudio.BroadcastAudioAsync(sender, audioData, audioData.Length);
    }
    
    public async Task HandleFullAudioTransmissionAsyncWebSockets(Client client, byte[] audioData)
    {
        string filename = $"full_audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{client.Id}.wav";
        await _gridFsManager.SaveAudioAsync(filename, audioData,true);

        Console.WriteLine(Constants.ReceivedFullAudioMessage, audioData.Length, client.Id);
    }

    public async Task HandleFullAudioTransmissionAsync(Client client, NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadAsync(lengthBuffer, 0, 4);
        int audioLength = BitConverter.ToInt32(lengthBuffer, 0);

        byte[] audioBuffer = new byte[audioLength];
        int bytesRead = 0;
        while (bytesRead < audioLength)
        {
            int chunkSize = await stream.ReadAsync(audioBuffer, bytesRead, audioLength - bytesRead);
            bytesRead += chunkSize;
        }

        string filename = $"full_audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{client.Id}.wav";
        await _gridFsManager.SaveAudioAsync(filename, audioBuffer,false);

        Console.WriteLine(Constants.ReceivedFullAudioMessage, audioLength, client.Id);
    }
}