using System.Net.WebSockets;
using server.Interface;
using server.Const;

namespace server.Classes.ClientHandler
{
    public class ClientManager : IClientManager
    {
        private readonly IEnumerable<Client> clients = new List<Client>();

        public void AddClient(Client client)
        {
            ((List<Client>)clients).Add(client);
            Console.WriteLine(Constants.ClientConnectedMessage, client.Id);
        }

        public void RemoveClient(string id)
        {
            var client = clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                ((List<Client>)clients).Remove(client);
                Console.WriteLine(Constants.ClientDisconnectedMessage, client.Id);
                
                client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, Constants.ClientDisconnectReason, CancellationToken.None).Wait();
            }
        }

        public IEnumerable<Client> GetAllClients()
        {
            return clients;
        }

        public void ListConnectedClients()
        {
            Console.WriteLine(Constants.ConnectedClientsListMessage, clients.Count());
            foreach (var client in clients)
            {
                Console.WriteLine(Constants.ClientInfoMessage, client.Id,client.OnOff, client.Frequency);
            }
        }
    }
}