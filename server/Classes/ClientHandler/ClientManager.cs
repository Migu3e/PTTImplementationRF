// ClientManager.cs

using System.Net;
using server.Const;
using server.Interface;

namespace server.Classes.ClientHandler;

public class ClientManager : IClientManager
{
    private readonly List<Client> clients = new List<Client>();

    public void AddClient(Client client)
    {
        clients.Add(client);
        Console.WriteLine(Constants.ClientConnectedMessage, client.Id);
    }

    public void RemoveClient(string id)
    {
        var client = clients.FirstOrDefault(c => c.Id == id);
        if (client != null)
        {
            clients.Remove(client);
            Console.WriteLine(Constants.ClientDisconnectedMessage, id);
            client.TcpClient.Close();
        }
    }

    public IEnumerable<Client> GetAllClients()
    {
        return clients;
    }

    public void ListConnectedClients()
    {
        Console.WriteLine(Constants.ConnectedClientsMessage, clients.Count);
        foreach (var client in clients)
        {
            Console.WriteLine(Constants.ClientInfoFormat, 
                client.Id, 
                ((IPEndPoint)client.TcpClient.Client.RemoteEndPoint).Address, 
                client.Channel);
        }
    }
}