using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using server.Interface;

namespace server.Classes.WebSocket
{
    public class WebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly IClientManager _clientManager;
        private bool _isRunning;

        public WebSocketServer(string url, IClientManager clientManager)
        {
            _url = url;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _clientManager = clientManager;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"WebSocket Server started on {_url}");

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

        public Task StopAsync()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("WebSocket Server stopped");
            return Task.CompletedTask;
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;

                var handler = new WebSocketHandler(_clientManager);
                await handler.HandleConnection(webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                if (webSocketContext?.WebSocket != null)
                {
                    webSocketContext.WebSocket.Dispose();
                }
            }
        }

    }
}