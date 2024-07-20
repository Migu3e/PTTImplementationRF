using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using server.Classes.ClientHandler;
using server.Const;
using server.Interface;

namespace server.Classes
{
    public class TcpServer : IServer
    {
        private readonly TcpListener listener;
        private readonly IClientManager clientManager;
        private readonly IReceiveAudio receiveAudio;
        private readonly IClientHandler clientHandler;
        private bool isRunning;

        public TcpServer(int port, IClientManager clientManager, IReceiveAudio receiveAudio, IClientHandler clientHandler)
        {
            listener = new TcpListener(IPAddress.Any, port);
            this.clientManager = clientManager;
            this.receiveAudio = receiveAudio;
            this.clientHandler = clientHandler;
        }

        public async Task RunAsync()
        {
            Console.WriteLine(Constants.ServerStartingMessage);
            Task serverTask = StartAsync();

            bool isRunning = true;
            while (isRunning)
            {
                isRunning = ServerOptions.HandleInput(clientManager);
                await Task.Delay(100);
            }

            Console.WriteLine(Constants.ServerStoppedMessage);
            await StopAsync();
            Console.ReadKey();
        }

        public async Task StartAsync()
        {
            listener.Start();
            Console.WriteLine($"{Constants.ServerStartedOnPort} {((IPEndPoint)listener.LocalEndpoint).Port}");
            isRunning = true;
            while (isRunning)
            {
                try
                {
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync();
                    string uniqueId = Guid.NewGuid().ToString();
                    var client = new Client(uniqueId, tcpClient);
                    clientManager.AddClient(client);
                    _ = clientHandler.HandleClientAsync(client, isRunning);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Constants.ErrorAcceptingClientMessage, ex.Message);
                }
            }
        }

        public Task StopAsync()
        {
            isRunning = false;
            listener.Stop();
            foreach (var client in clientManager.GetAllClients())
            {
                client.TcpClient.Close();
            }
            Console.WriteLine(Constants.ServerStoppedConsoleMessage);
            return Task.CompletedTask;
        }
    }
}