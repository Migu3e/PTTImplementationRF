using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using server.Interface;

namespace server.Classes.ClientHandler
{
    public class ClientManager : IClientManager
    {
        private readonly List<Client> clients = new List<Client>();

        public void AddClient(Client client)
        {
            clients.Add(client);
            Console.WriteLine($"client connected. Type: {(client.IsTcpClient ? "TCP" : "WebSocket")}, ID: {client.Id}");
        }

        public void RemoveClient(string id)
        {
            var client = clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                clients.Remove(client);
                Console.WriteLine($"client disconnected. Type: {(client.IsTcpClient ? "TCP" : "WebSocket")}, ID: {client.Id}");
                
                if (client.IsTcpClient)
                {
                    client.TcpClient?.Close();
                }
                else if (client.IsWebSocketClient)
                {
                    client.WebSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "client disconnected", CancellationToken.None).Wait();
                }
            }
        }

        public IEnumerable<Client> GetAllClients()
        {
            return clients;
        }

        public void ListConnectedClients()
        {
            Console.WriteLine($"connected clients ({clients.Count}):");
            foreach (var client in clients)
            {
                Console.WriteLine($"- Type: {(client.IsTcpClient ? "TCP" : "WebSocket")}, ID: {client.Id}, Channel: {client.Channel}");
            }
        }
    }
}