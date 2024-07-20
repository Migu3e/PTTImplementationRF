using server.Interface;

namespace server.Classes;

public class TransmitAudio : ITransmitAudio
{
    private readonly IClientManager clientManager;

    public TransmitAudio(IClientManager clientManager)
    {
        this.clientManager = clientManager;
    }

    public async Task BroadcastAudioAsync(string senderId, byte[] audioData, int length)
    {
        var tasks = clientManager.GetAllClients()
            .Where(c => c.Id != senderId)
            .Select(c => SendAudioToClientAsync(c, audioData, length));
        await Task.WhenAll(tasks);
    }

    public async Task SendAudioToClientAsync(Client client, byte[] audioData, int length)
    {
        try
        {
            await client.TcpClient.GetStream().WriteAsync(audioData, 0, length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Constants.ErrorSendingAudioMessage, client.Id, ex.Message);
        }
    }
}