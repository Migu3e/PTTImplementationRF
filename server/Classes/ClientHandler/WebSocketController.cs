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
    public class WebSocketController
    {
        private readonly IClientManager _clientManager;
        private readonly IReceiveAudio _receiveAudio;

        public WebSocketController(IClientManager clientManager, IReceiveAudio receiveAudio)
        {
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
        }

        public async Task HandleConnection(System.Net.WebSockets.WebSocket webSocket)
        {
            var shortId = Guid.NewGuid().ToString("").Substring(0,8);
            var client = new Client(shortId, webSocket);
            _clientManager.AddClient(client);

            await SendClientId(webSocket, client.Id);

            await ProcessMessages(webSocket, client);

            _clientManager.RemoveClient(client.Id);

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
    
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                    if (message.StartsWith("FRE|"))
                    {
                        string clientNewfRequency = message.Substring(4);
                        double frequency;
                        if (double.TryParse(clientNewfRequency, out frequency))
                        {
                            client.Frequency = frequency;
                            Console.WriteLine($"{client.Id} sent frequency: {frequency}");
                        }
                        else
                        {
                            Console.WriteLine("Error parsing frequency: {0}", clientNewfRequency);
                        }
                    }
                    if (message.StartsWith("VUL|"))
                    {
                        string clientNewVul = message.Substring(4);
                        double vulome;
                        
                        if (double.TryParse(clientNewVul, out vulome))
                        {
                            client.Volume = (int)vulome;
                            Console.WriteLine($"{client.Id} sent volume: {vulome}");
                        }
                        else
                        {
                            Console.WriteLine("Error parsing volume: {0}", clientNewVul);
                        }
                    }
                    
                    if (message.StartsWith("ONF|"))
                    {
                        string clientNewSettings = message.Substring(4);
                        Console.WriteLine($"{client.Id} sent option: {clientNewSettings}");
                        if (clientNewSettings == "ON")
                        {
                            client.OnOff = true;
                        }
                        else
                        {
                            client.OnOff = false;
                        }
                    }
            
                    if (result.EndOfMessage)
                    {
                        await ProcessAudioMessage(messageBuffer.ToArray(), messageBuffer.Count, client);
                        messageBuffer.Clear();
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
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

                int audioLength = count - 8;
                byte[] audioData = new byte[audioLength];
                Array.Copy(buffer, 8, audioData, 0, audioLength);


                Console.WriteLine($"{client.Id} send on {client.Frequency}");
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