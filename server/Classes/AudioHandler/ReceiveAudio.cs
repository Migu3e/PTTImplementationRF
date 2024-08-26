using server.Classes.ClientHandler;
using server.Classes.Logging;
using server.Const;
using server.Interface;

namespace server.Classes.AudioHandler;

public class ReceiveAudio : IReceiveAudio
{
    private readonly ITransmitAudio _transmitAudio;
    private readonly IGridFsManager _gridFsManager;
    private readonly LoggingService _loggingService;

    public ReceiveAudio(ITransmitAudio transmitAudio, IGridFsManager gridFsManager, LoggingService loggingService)
    {
        _transmitAudio = transmitAudio;
        _gridFsManager = gridFsManager;
        _loggingService = loggingService;
    }

    public async Task HandleRealtimeAudioAsyncWebSockets(Client sender, byte[] audioData)
    {
        await _transmitAudio.BroadcastAudioAsync(sender, audioData, audioData.Length);
    }
    
    public async Task HandleFullAudioTransmissionAsyncWebSockets(Client client, byte[] audioData)
    {
        string filename = $"full_audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{client.Id}.wav";
        await _gridFsManager.SaveAudioAsync(filename, audioData);

        Console.WriteLine(Constants.ReceivedFullAudioMessage, audioData.Length, client.Id);
    }
}