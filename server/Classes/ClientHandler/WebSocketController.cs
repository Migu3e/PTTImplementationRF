using System.Net.WebSockets;
using System.Text;
using server.Classes.ClientHandler;
using server.Interface;

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

            _clientManager.RemoveClient(client.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in HandleConnection: {e.Message}");
            throw;
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
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                if (result.Count >= 8 && buffer[0] == 0xFF && buffer[1] == 0xFF && buffer[2] == 0xFF &&
                    buffer[3] == 0xFF)
                {
                    // This is a full audio message
                    int audioLength = BitConverter.ToInt32(buffer, 4);
                    byte[] fullAudioData = new byte[audioLength];
                    Buffer.BlockCopy(buffer, 8, fullAudioData, 0, Math.Min(audioLength, result.Count - 8));

                    // If the audio data is larger than the buffer, we need to receive the rest
                    int receivedLength = Math.Min(audioLength, result.Count - 8);
                    while (receivedLength < audioLength)
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        Buffer.BlockCopy(buffer, 0, fullAudioData, receivedLength,
                            Math.Min(audioLength - receivedLength, result.Count));
                        receivedLength += result.Count;
                    }

                    Console.WriteLine($"Received full audio from client {client.Id}, length: {audioLength} bytes");
                    await _receiveAudio.HandleFullAudioTransmissionAsyncWebSockets(client, fullAudioData);
                }
                else
                {
                    // This is a real-time audio chunk
                    int sampleRate = BitConverter.ToInt32(buffer, 4);
                    byte[] audioData = new byte[result.Count - 8];
                    Buffer.BlockCopy(buffer, 8, audioData, 0, audioData.Length);

                    Console.WriteLine($"Received real-time audio from client {client.Id}, length: {audioData.Length} bytes, sample rate: {sampleRate}");
                    await _receiveAudio.HandleRealtimeAudioAsyncWebSockets(client, audioData);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received text message from client {client.Id}: {message}");
                // Handle text messages if needed
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by the client",
                    CancellationToken.None);
                break;
            }
        }
    }
}