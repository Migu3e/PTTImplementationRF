using System.Net.WebSockets;
using server.Classes.ClientHandler;
using server.Interface;

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
            .Where(client => client.Id != sender.Id && client.Channel == sender.Channel);

        var broadcastTasks = clientsOnSameChannel
            .Select(client => SendAudioToClientAsync(client, audioData, length, (byte)sender.Channel));

        await Task.WhenAll(broadcastTasks);
    }

    public async Task SendAudioToClientAsync(Client client, byte[] audioData, int length, byte channel)
    {
        try
        {
            int sampleRate = 44100;   
            byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, channel };
            byte[] sampleRateBytes = BitConverter.GetBytes(sampleRate);
            byte[] messageToSend = new byte[header.Length + sampleRateBytes.Length + length];
    
            Buffer.BlockCopy(header, 0, messageToSend, 0, header.Length);
            Buffer.BlockCopy(sampleRateBytes, 0, messageToSend, header.Length, sampleRateBytes.Length);
            Buffer.BlockCopy(audioData, 0, messageToSend, header.Length + sampleRateBytes.Length, length);


            if (client.WebSocket.State == WebSocketState.Open)
            {
                await client.WebSocket.SendAsync(new ArraySegment<byte>(messageToSend), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending audio to client {client.Id}: {ex.Message}");
        }
    }
}