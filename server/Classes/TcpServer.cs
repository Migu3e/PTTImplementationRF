using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Linq;

using System.Net;
using System.Net.Sockets;
using server.Classes;
using server.Interface;

public class TcpServer : IServer
{
    private readonly TcpListener listener;
    private readonly IClientManager clientManager;
    private readonly IGridFsManager gridFsManager;
    private bool isRunning;

    public TcpServer(int port, IClientManager clientManager, IGridFsManager gridFsManager)
    {
        listener = new TcpListener(IPAddress.Any, port);
        this.clientManager = clientManager;
        this.gridFsManager = gridFsManager;
    }

    public async Task StartAsync()
    {
        listener.Start();
        Console.WriteLine($"TCP Server started on port {((IPEndPoint)listener.LocalEndpoint).Port}");
        isRunning = true;
        while (isRunning)
        {
            try
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                string uniqueId = Guid.NewGuid().ToString();
                var client = new Client(uniqueId, tcpClient);
                clientManager.AddClient(client);
                _ = HandleClientAsync(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format(Constants.ErrorAcceptingClientMessage, ex.Message));
            }
        }
    }

    private async Task HandleClientAsync(Client client)
    {
        try
        {
            using (NetworkStream stream = client.TcpClient.GetStream())
            {
                byte[] headerBuffer = new byte[4];
                while (isRunning)
                {
                    int headerBytesRead = await stream.ReadAsync(headerBuffer, 0, 4);
                    if (headerBytesRead == 4)
                    {
                        if (headerBuffer[0] == 0xAA && headerBuffer[1] == 0xAA && headerBuffer[2] == 0xAA && headerBuffer[3] == 0xAA)
                        {
                            await HandleRealtimeAudioAsync(client, stream);
                        }
                        else if (headerBuffer[0] == 0xFF && headerBuffer[1] == 0xFF && headerBuffer[2] == 0xFF && headerBuffer[3] == 0xFF)
                        {
                            await HandleFullAudioTransmissionAsync(client, stream);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(Constants.ErrorHandlingClientMessage, client.Id, ex.Message));
        }
        finally
        {
            clientManager.RemoveClient(client.Id);
        }
    }

    private async Task HandleRealtimeAudioAsync(Client sender, NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadAsync(lengthBuffer, 0, 4);
        int audioLength = BitConverter.ToInt32(lengthBuffer, 0);

        byte[] audioBuffer = new byte[audioLength];
        int bytesRead = await stream.ReadAsync(audioBuffer, 0, audioLength);
        if (bytesRead == audioLength)
        {
            await BroadcastAudioAsync(sender.Id, audioBuffer, bytesRead);
        }
    }

    private async Task HandleFullAudioTransmissionAsync(Client client, NetworkStream stream)
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

        Console.WriteLine(string.Format(Constants.ReceivedFullAudioMessage, audioLength, client.Id));
    }

    private async Task BroadcastAudioAsync(string senderId, byte[] audioData, int length)
    {
        var tasks = clientManager.GetAllClients()
            .Where(c => c.Id != senderId)
            .Select(c => SendAudioToClientAsync(c, audioData, length));
        await Task.WhenAll(tasks);
    }

    private async Task SendAudioToClientAsync(Client client, byte[] audioData, int length)
    {
        try
        {
            await client.TcpClient.GetStream().WriteAsync(audioData, 0, length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(Constants.ErrorSendingAudioMessage, client.Id, ex.Message));
        }
    }

    public Task StopAsync()
    {
        isRunning = false;
        listener.Stop();
        foreach (var client in clientManager.GetAllClients())
        {
            client.TcpClient.Close();
        }
        Console.WriteLine(Constants.ServerStoppedConsoleMessage);
        return Task.CompletedTask;
    }
}