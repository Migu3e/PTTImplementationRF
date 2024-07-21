using server.Classes.ClientHandler;
using server.Const;
using server.Interface;

namespace server.Classes.AudioHandler;

public class TransmitAudio : ITransmitAudio
{
    private readonly IClientManager _clientManager;

    public TransmitAudio(IClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public async Task BroadcastAudioAsync(Client sender, byte[] audioData, int length)
    {
        var clientsOnSameChannel = _clientManager.GetAllClients()
            .Where(client => IsClientEligibleForBroadcast(client, sender));

        var broadcastTasks = clientsOnSameChannel
            .Select(client => SendAudioToClientAsync(client, audioData, length));

        await Task.WhenAll(broadcastTasks);
    }

    private bool IsClientEligibleForBroadcast(Client client, Client sender)
    {
        return client.Id != sender.Id && client.Channel == sender.Channel;
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