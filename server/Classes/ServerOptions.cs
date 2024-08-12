using server.Const;
using server.Interface;
using server.Classes.WebSocket;

namespace server.Classes;

public class ServerOptions
{
    private readonly IClientManager _clientManager;
    private readonly WebSocketServer _webSocketServer;

    public ServerOptions(IClientManager clientManager, WebSocketServer webSocketServer)
    {
        _clientManager = clientManager;
        _webSocketServer = webSocketServer;
    }

    public async Task<bool> HandleInput()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            switch (key.ToString().ToUpper())
            {
                case Constants.QuitCommand:
                    await _webSocketServer.StopAsync();
                    return false;
                case Constants.ListClientsCommand:
                    _clientManager.ListConnectedClients();
                    break;
                default:
                    Console.WriteLine(Constants.UnrecognizedKeyMessage, key, Constants.QuitCommand, Constants.ListClientsCommand);
                    break;
            }
        }
        return true;
    }
}