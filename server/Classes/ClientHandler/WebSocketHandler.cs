using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using server.Classes.ClientHandler;
using server.Interface;
using server.Const;

namespace server.Classes.WebSocket
{
    public class WebSocketHandler
    {
        private readonly IClientManager _clientManager;
        private readonly IReceiveAudio _receiveAudio;

        public WebSocketHandler(IClientManager clientManager, IReceiveAudio receiveAudio)
        {
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
        }

        public async Task HandleConnection(System.Net.WebSockets.WebSocket webSocket)
        {
            var client = new Client(Guid.NewGuid().ToString(), webSocket);
            _clientManager.AddClient(client);

            await SendClientId(webSocket, client.Id);

            try
            {
                await ProcessMessages(webSocket, client);
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

        private async Task ProcessMessages(System.Net.WebSockets.WebSocket webSocket, Client client)
        {
            var buffer = new byte[8192];
            var messageBuffer = new List<byte>();
    
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    messageBuffer.AddRange(buffer.Take(result.Count));
            
                    if (result.EndOfMessage)
                    {
                        await ProcessAudioMessage(messageBuffer.ToArray(), messageBuffer.Count, client);
                        messageBuffer.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by the client", CancellationToken.None);
                    Console.WriteLine(Constants.WebSocketConnectionClosed);
                    break;
                }
            }
        }

        private async Task ProcessAudioMessage(byte[] buffer, int count, Client client)
        {
            if (count < 8) 
            {
                Console.WriteLine(Constants.ShortMessageError, count);
                return;
            }

            if (buffer[0] == 0xAA && buffer[1] == 0xAA && buffer[2] == 0xAA)
            {
                byte channel = buffer[3];
        
                int audioLength = count - 8;
                byte[] audioData = new byte[audioLength];
                Array.Copy(buffer, 8, audioData, 0, audioLength);


                client.Channel = channel;
                await _receiveAudio.HandleRealtimeAudioAsyncWebSockets(client, audioData);
            }
            else 
            {
                int audioLength = BitConverter.ToInt32(buffer, 4);
                if (count < 8 + audioLength)
                {
                    Console.WriteLine(Constants.IncompleteFullAudioMessage, 8 + audioLength, count);
                    return;
                }

                byte[] audioData = new byte[audioLength];
                Array.Copy(buffer, 8, audioData, 0, audioLength);

                Console.WriteLine(Constants.ReceivedFullAudioInfo, audioLength);

                await _receiveAudio.HandleFullAudioTransmissionAsyncWebSockets(client, audioData);
            }
        }
    }
}