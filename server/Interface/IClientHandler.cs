
using server.Classes.ClientHandler;

namespace server.Interface
{
    public interface IClientHandler
    {
        Task HandleClientAsync(Client client, bool isRunning);
    }
}