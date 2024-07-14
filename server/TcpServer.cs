using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class TcpServer
{
    private TcpListener listener;
    private ConcurrentDictionary<string, ConnectedClient> connectedClients;
    private CancellationTokenSource cts;

    public TcpServer(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
        connectedClients = new ConcurrentDictionary<string, ConnectedClient>();
        cts = new CancellationTokenSource();
    }

    public Task StartAsync()
    {
        listener.Start();
        Console.WriteLine($"TCP Server started on port {((IPEndPoint)listener.LocalEndpoint).Port}");
        Console.Out.Flush(); // Force immediate display
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
                    Console.Out.Flush(); // Force immediate display
                    _ = HandleClientAsync(connectedClient);
                }
                else
                {
                    Console.WriteLine($"Failed to add client with ID: {uniqueId}");
                    Console.Out.Flush(); // Force immediate display
                    tcpClient.Close();
                }
            }
            catch (OperationCanceledException)
            {
                // Server is shutting down
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
                Console.Out.Flush(); // Force immediate display
                // Don't break here, allow the server to continue trying to accept new clients
            }
        }
    }
    private async Task HandleClientAsync(ConnectedClient client)
    {
        try
        {
            // Here you can implement any specific logic for handling the client
            // For now, we'll just keep the connection open
            await Task.Delay(-1, cts.Token); // Wait indefinitely or until cancellation
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