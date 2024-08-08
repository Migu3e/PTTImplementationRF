using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Const;
using server.GridFS;
using server.Interface;

class Program
{
    static async Task Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var clientManager = new ClientManager();
        var gridFsManager = new GridFsManager(baseDirectory);
        var transmitAudio = new TransmitAudio(clientManager);
        var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

        IClientHandler clientHandler = new ClientHandler(clientManager, receiveAudio);

        var server = new TcpServer(Constants.ServerPort, clientManager, receiveAudio, clientHandler);
        
        // da TCP server
        _ = server.StartAsync();

        // the WebSocket server
        var webSocketServerTask = RunWebSocketServerAsync();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        await server.StopAsync();
        await webSocketServerTask;
    }

    static async Task RunWebSocketServerAsync()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8081/");
        listener.Start();
        Console.WriteLine("WebSocket Server is listeinng on port 8081");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                _ = ProcessWebSocketRequestAsync(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
    {
        WebSocketContext webSocketContext = null;

        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = webSocketContext.WebSocket;

            Console.WriteLine("Client connected");

            byte[] buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    string receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"reseive: {receivedMessage}");
                    
                    string responseMessage = "back: " + receivedMessage;
                    await webSocket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(responseMessage)), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine($"sent: {responseMessage}");
                }
            }
        }
        catch (WebSocketException Ex)
        {
            Console.WriteLine($"WebSocket error: {Ex.Message}");
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
            Console.WriteLine("client disconnected");
        }
    }
}
