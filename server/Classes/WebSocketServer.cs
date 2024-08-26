using System;
using System.Net;
using System.Net.WebSockets;
using System.Net.Sockets;
using server.Classes.Logging;
using server.Const;
using server.Interface;

namespace server.Classes.WebSocket
{
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly IClientManager _clientManager;
        private bool _isRunning;
        private readonly IReceiveAudio _receiveAudio;
        private readonly LoggingService _loggingService;

        public WebSocketServer(int port, IClientManager clientManager, ITransmitAudio transmitAudio, IReceiveAudio receiveAudio, LoggingService loggingService)
        {
            _url = $"http://*:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
            _loggingService = loggingService;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"{Constants.ServerAccessible} {GetServerAddress()}");

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

        //LAN IP address
        public string GetServerAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return $"http://{ip}:{Constants.WebSocketServerPort}/";
                }
            }
            throw new Exception();
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;

                var handler = new WebSocketController(_clientManager, _receiveAudio, _loggingService);
                await handler.HandleConnection(webSocket);
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