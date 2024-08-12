using System;
using System.Threading.Tasks;
using GridFs;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.WebSocket;
using server.Const;
using server.Interface;

class Program
{
    static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var clientManager = new ClientManager();
        var gridFsManager = new GridFsManager(baseDirectory);
        var transmitAudio = new TransmitAudio(clientManager);
        var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

        // WebSocket server
        var webSocketServer = new WebSocketServer($"http://localhost:{Constants.WebSocketServerPort}/", clientManager, transmitAudio, receiveAudio);

        var serverOptions = new ServerOptions(clientManager, webSocketServer);

        var webSocketServerTask = webSocketServer.StartAsync();

        Console.WriteLine("WebSocket Server started. Press 'Q' to quit or 'L' to list connected clients.");

        bool isRunning = true;
        while (isRunning)
        {
            isRunning = await serverOptions.HandleInput();
            await Task.Delay(100);
        }

        Console.WriteLine("Server stopped.");

        // Wait for the server task to complete
        await webSocketServerTask;
    }
}