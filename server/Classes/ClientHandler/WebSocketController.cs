using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using server.Classes.ClientHandler;
using server.Interface;
using server.Const;

namespace server.Classes.WebSocket
{
    public class WebSocketController
    {
        private readonly IClientManager _clientManager;
        private readonly IReceiveAudio _receiveAudio;
        private readonly IMongoDatabase _database;

        
        public WebSocketController(IClientManager clientManager, IReceiveAudio receiveAudio,IMongoDatabase database)
        {
            _clientManager = clientManager;
            _receiveAudio = receiveAudio;
            _database = database;

        }

        public async Task HandleConnection(System.Net.WebSockets.WebSocket webSocket)
        {
            try
            {
                // wait for the client to send its ID
                var buffer = new byte[90024 * 4];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var clientId = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                Console.WriteLine($"Received client ID: {clientId}");

                var client = new Client(clientId, webSocket,_database);
                _clientManager.AddClient(client);

                await SendConnectionConfirmation(webSocket);

                await ProcessMessages(webSocket, client);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in HandleConnection: {e.Message}");
            }
            finally
            {
                if (_clientManager.GetAllClients().Any(c => c.WebSocket == webSocket))
                {
                    _clientManager.RemoveClient(_clientManager.GetAllClients().First(c => c.WebSocket == webSocket).Id);
                }
            }
        }

        private async Task SendConnectionConfirmation(System.Net.WebSockets.WebSocket webSocket)
        {
            var confirmationMessage = "Connected";
            var bytes = Encoding.UTF8.GetBytes(confirmationMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ProcessMessages(System.Net.WebSockets.WebSocket webSocket, Client client)
        {
            var buffer = new byte[900024 * 16];  // Increased buffer size
            var cancelToken = new CancellationTokenSource();
            var messageBuffer = new List<byte>();

            // Start a heartbeat task

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        messageBuffer.AddRange(buffer.Take(result.Count));

                        // if the message is complete
                        if (result.EndOfMessage)
                        {
                            await ProcessAudioMessage(messageBuffer.ToArray(),result.Count, client);
                            messageBuffer.Clear();
                        }                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    }
                }
            }
            catch (WebSocketException e)
            {
                Console.WriteLine($"WebSocket error for client {client.Id}: {e.Message}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing messages for client {client.Id}: {e.Message}");
            }
            finally
            {
                cancelToken.Cancel();
            }
        }

        private async Task ProcessAudioMessage(byte[] buffer, int count, Client client)
        {
            if (count < 8) 
            {
                Console.WriteLine(Constants.ShortMessageError, count);
                return;
            }

            if (buffer[0] == 0xAA && buffer[1] == 0xAA && buffer[2] == 0xAA && buffer[3] == 0xAA)
            {
                int audioLength = count - 8;
                byte[] audioData = new byte[audioLength];
                Array.Copy(buffer, 8, audioData, 0, audioLength);

                Console.WriteLine($"{client.Id} send on {client.Frequency}");
                await _receiveAudio.HandleRealtimeAudioAsyncWebSockets(client, audioData);
            }
            else if (buffer[0] == 0xFF && buffer[1] == 0xFF && buffer[2] == 0xFF && buffer[3] == 0xFF)
            {
                int expectedLength = BitConverter.ToInt32(buffer, 4);
                int actualLength = count - 8;

                if (actualLength < expectedLength)
                {
                    Console.WriteLine("Received incomplete audio data");
                }

                byte[] audioData = new byte[expectedLength];
                Array.Copy(buffer, 8, audioData, 0, expectedLength);

                Console.WriteLine($"Received full audio: {expectedLength} bytes from client {client.Id}");

                await _receiveAudio.HandleFullAudioTransmissionAsyncWebSockets(client, audioData);
            }
        }
    }

        
    
}