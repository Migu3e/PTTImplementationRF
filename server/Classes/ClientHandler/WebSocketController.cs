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
            try
            {
                // Wait for the client to send its ID
                var buffer = new byte[1024 * 4];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var clientId = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

                Console.WriteLine($"Received client ID: {clientId}");

                var client = new Client(clientId, webSocket);
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
            var buffer = new byte[2024 * 16];  // Increased buffer size
            var cancelToken = new CancellationTokenSource();

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
                        await HandleBinaryMessage(client, buffer, result.Count);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received text message from client {client.Id}: {message}");
                        // Handle text messages if needed
                    }
                }
            }
            catch (WebSocketException e)
            {
                Console.WriteLine($"WebSocket error for client {client.Id}: {e.Message}");
            }
            catch (OperationCanceledException)
            {
                // This is expected when the token is canceled
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

        private async Task HandleBinaryMessage(Client client, byte[] buffer, int count)
        {
            try
            {
                if (count >= 8 && buffer[0] == 0xFF && buffer[1] == 0xFF && buffer[2] == 0xFF && buffer[3] == 0xFF)
                {
                    // Full audio message
                    int audioLength = BitConverter.ToInt32(buffer, 4);
                    if (audioLength > 0 && audioLength <= buffer.Length - 8)
                    {
                        byte[] audioData = new byte[audioLength];
                        Buffer.BlockCopy(buffer, 8, audioData, 0, audioLength);
                        await _receiveAudio.HandleFullAudioTransmissionAsyncWebSockets(client, audioData);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid audio length: {audioLength}");
                    }
                }
                else
                {
                    // Real-time audio chunk
                    await _receiveAudio.HandleRealtimeAudioAsyncWebSockets(client, new ArraySegment<byte>(buffer, 0, count).ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error handling binary message for client {client.Id}: {e.Message}");
            }
        }

        
    }
}