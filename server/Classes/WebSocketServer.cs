using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MongoDB.Driver;
using server.Classes.AudioHandler;
using server.Const;
using server.Interface;

namespace server.Classes.WebSocket
{
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly IClientManager _clientManager;
        private readonly ITransmitAudio _transmitAudio;
        private readonly IReceiveAudio _receiveAudio;
        private bool _isRunning;
        private readonly IMongoDatabase _database;


        public WebSocketServer(int port, IClientManager clientManager, ITransmitAudio transmitAudio, IReceiveAudio receiveAudio, IMongoDatabase database)
        {
            _url = $"http://*:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _clientManager = clientManager;
            _transmitAudio = transmitAudio;
            _receiveAudio = receiveAudio;
            _database = database;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"{Constants.WebServerStartedOn}{_url}");

            while (_isRunning)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    ProcessWebSocketRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;

                var controller = new WebSocketController(_clientManager, _receiveAudio,_database);
                await controller.HandleConnection(webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                if (webSocketContext?.WebSocket != null)
                {
                    webSocketContext.WebSocket.Dispose();
                }
            }
        }

        public Task StopAsync()
        {
            _isRunning = false;
            _listener.Stop();
            return Task.CompletedTask;
        }
    }
}