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

        IClientHandler clientHandler = new ClientHandler(clientManager, receiveAudio);

        // TCP server
        var tcpServer = new TcpServer(Constants.TcpServerPort, clientManager, receiveAudio, clientHandler);
        
        // WebSocket server
        var webSocketServer = new WebSocketServer($"http://localhost:{Constants.WebSocketServerPort}/", clientManager,transmitAudio,receiveAudio);

        var tcpServerTask = tcpServer.RunAsync();
        var webSocketServerTask = webSocketServer.StartAsync();

        Console.ReadKey();

        await tcpServer.StopAsync();
        await webSocketServer.StopAsync();

        // server tasks need to complete
        await Task.WhenAll(tcpServerTask, webSocketServerTask);
    }
}