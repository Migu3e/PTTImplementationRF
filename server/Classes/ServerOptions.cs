using server.Const;
using server.Interface;

namespace server.Classes;

public class ServerOptions
{
    public static bool HandleInput(IClientManager clientManager)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            switch (key.ToString().ToUpper())
            {
                case Constants.QuitCommand:
                    return false;
                case Constants.ListClientsCommand:
                    clientManager.ListConnectedClients();
                    break;
                default:
                    Console.WriteLine(Constants.UnrecognizedKeyMessage, key, Constants.QuitCommand, Constants.ListClientsCommand);
                    break;
            }
        }
        return true;
    }
}