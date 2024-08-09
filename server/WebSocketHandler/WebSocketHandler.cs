using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using server.Classes.ClientHandler;
using server.Interface;

namespace server.Classes.WebSocket
{
    public class WebSocketHandler
    {
        private readonly IClientManager _clientManager;

        public WebSocketHandler(IClientManager clientManager)
        {
            _clientManager = clientManager;
        }

        public async Task HandleConnection(System.Net.WebSockets.WebSocket webSocket)
        {
            var client = new Client(Guid.NewGuid().ToString(), webSocket);
            _clientManager.AddClient(client);

            // snd client ID to client
            await SendClientId(webSocket, client.Id);

            try
            {
                await KeepConnectionAlive(webSocket);
            }
            finally
            {
                _clientManager.RemoveClient(client.Id);
            }
        }

        private async Task SendClientId(System.Net.WebSockets.WebSocket webSocket, string clientId)
        {
            var bytes = Encoding.UTF8.GetBytes(clientId);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }


        private async Task KeepConnectionAlive(System.Net.WebSockets.WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by the client", CancellationToken.None);
                }
            }
        }
    }
}