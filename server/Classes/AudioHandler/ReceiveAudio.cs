using System.Net.Sockets;
using server.Classes.ClientHandler;
using server.Const;
using server.Interface;

namespace server.Classes.AudioHandler;

public class ReceiveAudio : IReceiveAudio
{
    private readonly ITransmitAudio transmitAudio;
    private readonly IGridFsManager gridFsManager;

    public ReceiveAudio(ITransmitAudio transmitAudio, IGridFsManager gridFsManager)
    {
        this.transmitAudio = transmitAudio;
        this.gridFsManager = gridFsManager;
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
            await transmitAudio.BroadcastAudioAsync(sender, audioBuffer, bytesRead);
        }
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
        await gridFsManager.SaveAudioAsync(filename, audioBuffer);

        Console.WriteLine(Constants.ReceivedFullAudioMessage, audioLength, client.Id);
    }
}