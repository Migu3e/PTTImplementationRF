using System.Net.WebSockets;
using System.Text;
using server.Classes.ClientHandler;
using server.Classes.Logging;
using server.Interface;
using server.Const;

namespace server.Classes.WebSocket
{
    public class WebSocketController
    {
        private readonly IClientManager _clientManager;
        private readonly IReceiveAudio _receiveAudio;
        private readonly LoggingService _loggingService;

        public WebSocketController(IClientManager clientManager, IReceiveAudio receiveAudio, LoggingService loggingService)
        {
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
            _loggingService = loggingService;
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
                            //spam thingy
                            if (client.Frequency != frequency)
                            {
                                client.Frequency = frequency;
                                await _loggingService.LogClientAction(client.Id, $"moved to frequency {frequency}");
                                await _loggingService.LogServerAction($"Client {client.Id} changed frequency to {frequency}");
                                Console.WriteLine(Constants.ReceivedOptionFrequency, client.Id,client.Frequency);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{Constants.ErrorInFrequency}: {clientNewfRequency}");
                        }
                    }
                    if (message.StartsWith("VUL|"))
                    {
                        string clientNewVul = message.Substring(4);
                        double vulome;
                        
                        if (double.TryParse(clientNewVul, out vulome))
                        {
                            if (client.Volume != vulome)
                            {
                                client.Volume = (int)vulome;
                                await _loggingService.LogClientAction(client.Id, $"changed volume to {vulome}");
                                await _loggingService.LogServerAction($"Client {client.Id} change volume to {vulome}");
                                Console.WriteLine(Constants.ReceivedOptionVolume, client.Id,client.Volume);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{Constants.ErrorInVolume}: {clientNewVul}");
                        }
                    }
                    
                    if (message.StartsWith("ONF|"))
                    {
                        string clientNewSettings = message.Substring(4);
                        Console.WriteLine(Constants.ReceivedOptionOnOff,client.Id,clientNewSettings);
                        if (clientNewSettings == "ON")
                        {
                            client.OnOff = true;
                            await _loggingService.LogClientAction(client.Id, "turned on");
                            await _loggingService.LogServerAction($"Client {client.Id} turned on");


                        }
                        else if (clientNewSettings == "OFF")
                        {
                            client.OnOff = false;
                            await _loggingService.LogClientAction(client.Id, "turned off");
                            await _loggingService.LogServerAction($"Client {client.Id} turned off");

                        }
                        else
                        {
                            Console.WriteLine($"{Constants.ErrorInVolume}: {Constants.ErrorInOnOff}");
                        }
                        // im ze lo oved ani efga be misho
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


                await _loggingService.LogClientAction(client.Id, "full message arrived");
                await _receiveAudio.HandleFullAudioTransmissionAsyncWebSockets(client, audioData);
                
            }
        }
    }
}