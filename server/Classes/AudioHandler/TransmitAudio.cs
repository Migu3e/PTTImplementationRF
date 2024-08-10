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
            byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, channel };
            byte[] lengthBytes = BitConverter.GetBytes(length);
            byte[] messageToSend = new byte[header.Length + lengthBytes.Length + length];
        
            Buffer.BlockCopy(header, 0, messageToSend, 0, header.Length);
            Buffer.BlockCopy(lengthBytes, 0, messageToSend, header.Length, lengthBytes.Length);
            Buffer.BlockCopy(audioData, 0, messageToSend, header.Length + lengthBytes.Length, length);

            Console.WriteLine($"Sending to client {client.Id}: Length {length}, Channel {channel}, Total message size {messageToSend.Length}");
        
            if (client.WebSocket != null && client.WebSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await client.WebSocket.SendAsync(new ArraySegment<byte>(messageToSend), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            else if (client.TcpClient != null && client.TcpClient.Connected)
            {
                await client.TcpClient.GetStream().WriteAsync(messageToSend, 0, messageToSend.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(client.Id, ex.Message);
        }
    }

}