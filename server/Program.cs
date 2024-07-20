using server.Classes;
using server.Interface;

class Program
{
    static async Task Main(string[] args)
    {
        var clientManager = new ClientManager();
        var gridFsManager = new GridFsManager(Constants.MongoConnectionString, Constants.DatabaseName);
        var server = new TcpServer(Constants.ServerPort, clientManager, gridFsManager);

        await RunServerAsync(server, clientManager);
    }

    private static async Task RunServerAsync(IServer server, IClientManager clientManager)
    {
        Console.WriteLine(Constants.ServerStartingMessage);
        Task serverTask = server.StartAsync();

        bool isRunning = true;
        while (isRunning)
        {
            isRunning = ServerOptions.HandleInput(clientManager);
            await Task.Delay(100);
        }

        Console.WriteLine(Constants.ServerStoppedMessage);
        await server.StopAsync();
        Console.ReadKey();
    }
}