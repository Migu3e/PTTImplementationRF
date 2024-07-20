using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client.Interfaces
{
    public interface IClientInputOptions
    {
        Task HandleInput(ConsoleKey key, NetworkStream stream);
        bool ShouldExit();
    }
}