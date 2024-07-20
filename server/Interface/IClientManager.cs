using System.Net.Sockets;
using server.Classes;

namespace server.Interface;

public interface IClientManager
{
    void AddClient(Client client);
    void RemoveClient(string id);
    IEnumerable<Client> GetAllClients();
    void ListConnectedClients();
}