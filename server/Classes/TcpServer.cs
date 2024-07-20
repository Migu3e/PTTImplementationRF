using System.Net;
using System.Net.Sockets;
using server.Const;
using server.Interface;

namespace server.Classes;

public class TcpServer : IServer
{
    private readonly TcpListener listener;
    private readonly IClientManager clientManager;
    private readonly IReceiveAudio receiveAudio;
    private bool isRunning;

    public TcpServer(int port, IClientManager clientManager, IReceiveAudio receiveAudio)
    {
        listener = new TcpListener(IPAddress.Any, port);
        this.clientManager = clientManager;
        this.receiveAudio = receiveAudio;
    }

    public async Task RunAsync()
    {
        Console.WriteLine(Constants.ServerStartingMessage);
        Task serverTask = StartAsync();

        bool isRunning = true;
        while (isRunning)
        {
            isRunning = ServerOptions.HandleInput(clientManager);
            await Task.Delay(100);
        }

        Console.WriteLine(Constants.ServerStoppedMessage);
        await StopAsync();
        Console.ReadKey();
    }

    public async Task StartAsync()
    {
        listener.Start();
        Console.WriteLine($"{Constants.ServerStartedOnPort} {((IPEndPoint)listener.LocalEndpoint).Port}");
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
                Console.WriteLine(Constants.ErrorAcceptingClientMessage, ex.Message);
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
                            await receiveAudio.HandleRealtimeAudioAsync(client, stream);
                        }
                        else if (headerBuffer[0] == 0xFF && headerBuffer[1] == 0xFF && headerBuffer[2] == 0xFF && headerBuffer[3] == 0xFF)
                        {
                            await receiveAudio.HandleFullAudioTransmissionAsync(client, stream);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(Constants.ErrorHandlingClientMessage, client.Id, ex.Message);
        }
        finally
        {
            clientManager.RemoveClient(client.Id);
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