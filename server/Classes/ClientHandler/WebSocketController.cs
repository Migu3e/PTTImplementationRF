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
                    await ProcessTextMessage(client, message);
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
                    break;
                }
            }
        }

        private async Task ProcessTextMessage(Client client, string message)
        {
            var parts = message.Split('|');
            if (parts.Length != 2)
            {
                Console.WriteLine($"Invalid message format: {message}");
                return;
            }

            var command = parts[0];
            var value = parts[1];

            switch (command)
            {
                case "FRE":
                    await ProcessFrequencyChange(client, value);
                    break;
                case "VUL":
                    await ProcessVolumeChange(client, value);
                    break;
                case "ONF":
                    await ProcessOnOffChange(client, value);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }

        private async Task ProcessFrequencyChange(Client client, string frequencyString)
        {
            if (double.TryParse(frequencyString, out double frequency))
            {
                if (client.Frequency != frequency)
                {
                    client.Frequency = frequency;
                    await _loggingService.LogClientAction(client.Id, $"moved to frequency {frequency}");
                    await _loggingService.LogServerAction($"Client {client.Id} changed frequency to {frequency}");
                    Console.WriteLine(Constants.ReceivedOptionFrequency, client.Id, client.Frequency);
                }
            }
            else
            {
                Console.WriteLine($"{Constants.ErrorInFrequency}: {frequencyString}");
            }
        }

        private async Task ProcessVolumeChange(Client client, string volumeString)
        {
            if (double.TryParse(volumeString, out double volume))
            {
                if (client.Volume != volume)
                {
                    client.Volume = (int)volume;
                    await _loggingService.LogClientAction(client.Id, $"changed volume to {volume}");
                    await _loggingService.LogServerAction($"Client {client.Id} change volume to {volume}");
                    Console.WriteLine(Constants.ReceivedOptionVolume, client.Id, client.Volume);
                }
            }
            else
            {
                Console.WriteLine($"{Constants.ErrorInVolume}: {volumeString}");
            }
        }

        private async Task ProcessOnOffChange(Client client, string stateString)
        {
            if (stateString == "ON" || stateString == "OFF")
            {
                bool newState = stateString == "ON";
                if (client.OnOff != newState)
                {
                    client.OnOff = newState;
                    string stateDescription = newState ? "turned on" : "turned off";
                    await _loggingService.LogClientAction(client.Id, stateDescription);
                    await _loggingService.LogServerAction($"Client {client.Id} {stateDescription}");
                    Console.WriteLine(Constants.ReceivedOptionOnOff, client.Id, stateString);
                }
            }
            else
            {
                Console.WriteLine($"{Constants.ErrorInOnOff}: {stateString}");
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