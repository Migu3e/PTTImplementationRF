using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Linq;

public class TcpServer
{
    private TcpListener listener;
    private ConcurrentDictionary<string, ConnectedClient> connectedClients;
    private CancellationTokenSource cts;
    private IMongoDatabase database;
    private IGridFSBucket gridFS;

    public TcpServer(int port, string mongoConnectionString, string databaseName)
    {
        listener = new TcpListener(IPAddress.Any, port);
        connectedClients = new ConcurrentDictionary<string, ConnectedClient>();
        cts = new CancellationTokenSource();

        var client = new MongoClient(mongoConnectionString);
        database = client.GetDatabase(databaseName);
        gridFS = new GridFSBucket(database);
    }

    public Task StartAsync()
    {
        listener.Start();
        Console.WriteLine($"TCP Server started on port {((IPEndPoint)listener.LocalEndpoint).Port}");
        Console.Out.Flush();
        return AcceptClientsAsync();
    }

    private async Task AcceptClientsAsync()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                string uniqueId = Guid.NewGuid().ToString();
                var connectedClient = new ConnectedClient(uniqueId, tcpClient);
            
                if (connectedClients.TryAdd(uniqueId, connectedClient))
                {
                    Console.WriteLine($"Client connected. Assigned ID: {uniqueId}");
                    Console.Out.Flush();
                    _ = HandleClientAsync(connectedClient);
                }
                else
                {
                    Console.WriteLine($"Failed to add client with ID: {uniqueId}");
                    Console.Out.Flush();
                    tcpClient.Close();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
                Console.Out.Flush();
            }
        }
    }
    private async Task HandleClientAsync(ConnectedClient client)
    {
        try
        {
            using (NetworkStream stream = client.TcpClient.GetStream())
            {
                byte[] headerBuffer = new byte[4];
                while (!cts.Token.IsCancellationRequested)
                {
                    int headerBytesRead = await stream.ReadAsync(headerBuffer, 0, 4, cts.Token);
                    if (headerBytesRead == 4)
                    {
                        if (headerBuffer[0] == 0xAA && headerBuffer[1] == 0xAA && headerBuffer[2] == 0xAA && headerBuffer[3] == 0xAA)
                        {
                            // Read the length of the audio data
                            byte[] lengthBuffer = new byte[4];
                            await stream.ReadAsync(lengthBuffer, 0, 4, cts.Token);
                            int audioLength = BitConverter.ToInt32(lengthBuffer, 0);

                            // Real-time audio transmission
                            byte[] audioBuffer = new byte[audioLength];
                            int bytesRead = await stream.ReadAsync(audioBuffer, 0, audioLength, cts.Token);
                            if (bytesRead == audioLength)
                            {
                                // Broadcast to other clients, but don't save
                                await BroadcastAudioAsync(client.Id, audioBuffer, bytesRead);
                            }
                        }
                        else if (headerBuffer[0] == 0xFF && headerBuffer[1] == 0xFF && headerBuffer[2] == 0xFF && headerBuffer[3] == 0xFF)
                        {
                            // Full audio transmission
                            await HandleFullAudioTransmission(client, stream);
                        }
                    }
                }
            }
        }

        catch (OperationCanceledException)
        {
            // Server is shutting down
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client {client.Id}: {ex.Message}");
        }
        finally
        {
            // Remove the client when the connection is closed
            if (connectedClients.TryRemove(client.Id, out _))
            {
                Console.WriteLine($"Client disconnected. ID: {client.Id}");
                client.TcpClient.Close();
            }
        }
    }
    private async Task HandleFullAudioTransmission(ConnectedClient client, NetworkStream stream)
    {
        try
        {
            // Read the length of the audio data
            byte[] lengthBuffer = new byte[4];
            await stream.ReadAsync(lengthBuffer, 0, 4);
            int audioLength = BitConverter.ToInt32(lengthBuffer, 0);

            // Read the full audio data
            byte[] audioBuffer = new byte[audioLength];
            int bytesRead = 0;
            while (bytesRead < audioLength)
            {
                int chunkSize = await stream.ReadAsync(audioBuffer, bytesRead, audioLength - bytesRead);
                bytesRead += chunkSize;
            }

            // Create a directory to store audio files if it doesn't exist
            string audioDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioFiles");
            Directory.CreateDirectory(audioDirectory);

            // Generate a unique filename
            string filename = $"full_audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{client.Id}.wav";
            string filePath = Path.Combine(audioDirectory, filename);

            // Save the audio file
            await File.WriteAllBytesAsync(filePath, audioBuffer);

            Console.WriteLine($"Received and saved full audio ({audioLength} bytes) from client {client.Id}");
            Console.WriteLine($"File saved at: {filePath}");

            // Optionally, you can broadcast this audio to other clients
            // await BroadcastAudioAsync(client.Id, audioBuffer, audioLength);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling full audio transmission from client {client.Id}: {ex.Message}");
        }
    }

    private async Task BroadcastAudioAsync(string senderId, byte[] audioData, int length)
    {
        var tasks = connectedClients.Where(kvp => kvp.Key != senderId)
                                    .Select(kvp => SendAudioToClientAsync(kvp.Value, audioData, length));
        await Task.WhenAll(tasks);
    }

    private async Task SendAudioToClientAsync(ConnectedClient client, byte[] audioData, int length)
    {
        try
        {
            await client.TcpClient.GetStream().WriteAsync(audioData, 0, length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending audio to client {client.Id}: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        cts.Cancel();
        listener.Stop();

        foreach (var client in connectedClients.Values)
        {
            client.TcpClient.Close();
        }
        connectedClients.Clear();

        Console.WriteLine("TCP Server stopped");
    }

    public void ListConnectedClients()
    {
        Console.WriteLine($"Connected clients ({connectedClients.Count}):");
        foreach (var client in connectedClients.Values)
        {
            Console.WriteLine($"- ID: {client.Id}, IP: {((IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address}");
        }
    }
}