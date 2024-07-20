// ClientManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using server.Interface;

namespace server.Classes;

using System.Net;

public class ClientManager : IClientManager
{
    private readonly List<Client> clients = new List<Client>();

    public void AddClient(Client client)
    {
        clients.Add(client);
        Console.WriteLine(string.Format(Constants.ClientConnectedMessage, client.Id));
    }

    public void RemoveClient(string id)
    {
        var client = clients.FirstOrDefault(c => c.Id == id);
        if (client != null)
        {
            clients.Remove(client);
            Console.WriteLine(string.Format(Constants.ClientDisconnectedMessage, id));
            client.TcpClient.Close();
        }
    }

    public IEnumerable<Client> GetAllClients()
    {
        return clients;
    }

    public void ListConnectedClients()
    {
        Console.WriteLine(string.Format(Constants.ConnectedClientsMessage, clients.Count));
        foreach (var client in clients)
        {
            Console.WriteLine(string.Format(Constants.ClientInfoFormat, 
                client.Id, 
                ((IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address, 
                client.Channel));
        }
    }
}