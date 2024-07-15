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
                byte[] buffer = new byte[4096]; // Increased buffer size
                while (!cts.Token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    if (bytesRead > 0)
                    {
                        // Save audio to GridFS
                        string filename = $"audio_{DateTime.UtcNow:yyyyMMddHHmmss}_{client.Id}.raw";
                        await gridFS.UploadFromBytesAsync(filename, buffer.Take(bytesRead).ToArray());

                        // Broadcast to other clients
                        await BroadcastAudioAsync(client.Id, buffer, bytesRead);
                    }
                }
            }
        }
        // ... rest of the method remains the same
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