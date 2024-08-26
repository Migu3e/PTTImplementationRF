using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GridFs;
using MongoDB.Driver;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.WebSocket;
using server.Const;
using server.Enums;
using server.Interface;

class Program
{
    static async Task Main(string[] args)
    {
        var mongoClient = new MongoClient(Constants.MongoConnectionString);
        var database = mongoClient.GetDatabase(Constants.DatabaseName);

        var clientManager = new ClientManager();
        var clientSettingsService = new ClientSettingsService(database);
        var gridFsManager = new GridFsManager(database);
        var transmitAudio = new TransmitAudio(clientManager);
        var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

        var webSocketServer = new WebSocketServer(Constants.WebSocketServerPort, clientManager, transmitAudio, receiveAudio);
        var webSocketServerTask = webSocketServer.StartAsync();

        // Start HTTP listener
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5000/");
        httpListener.Start();

        Console.WriteLine(Constants.StartedConnection);
        Console.WriteLine($"{Constants.ServerConnectionPoint} http://localhost:5000");

        // Handle HTTP requests
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var context = await httpListener.GetContextAsync();
                _ = HandleHttpRequestAsync(context, clientManager, clientSettingsService);
            }
        });

        await webSocketServerTask;
    }
static async Task HandleHttpRequestAsync(HttpListenerContext context, IClientManager clientManager, ClientSettingsService clientSettingsService)
{
    var request = context.Request;
    var response = context.Response;

    try
    {
        Console.WriteLine($"Received {request.HttpMethod} request at {request.Url.AbsolutePath}");

        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "GET, PUT, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
        response.AddHeader("Cache-Control", "post-check=0, pre-check=0");
        response.AddHeader("Pragma", "no-cache");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            Console.WriteLine("Handled OPTIONS request");
            response.Close();
            return;
        }

        if (request.HttpMethod == "PUT" && request.Url.AbsolutePath.StartsWith("/api/client/"))
        {
            Console.WriteLine("Entering PUT request handling");
            var path = request.Url.AbsolutePath;
            Console.WriteLine(path);
            var pathParts = path.Trim('/').Split('/');

            Console.WriteLine(pathParts);
            if (pathParts.Length == 4 && pathParts[3] == "settings")
            {
                var clientId = pathParts[2];
                var client = clientManager.GetAllClients().FirstOrDefault(c => c.Id == clientId);
                if (client == null)
                {
                    response.StatusCode = 404;
                    Console.WriteLine($"Client with ID {clientId} not found");
                    response.Close();
                    return;
                }

                try
                {
                    Console.WriteLine("hello");
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        var body = await reader.ReadToEndAsync();
                        Console.WriteLine($"Request body: {body}");
                        Console.WriteLine($"Request method: {request.HttpMethod}");
                        Console.WriteLine($"Request URL: {request.Url.AbsolutePath}");
                        Console.WriteLine($"Request Content-Type: {request.Headers["Content-Type"]}");


                        var parsedBody = HttpUtility.ParseQueryString(body);

                        if (Enum.TryParse<FrequencyChannel>(parsedBody["channel"], out var channel))
                        {
                            client.Channel = channel;
                            Console.WriteLine($"Updated channel to {channel} for client {clientId}");
                        }

                        if (int.TryParse(parsedBody["volume"], out var volume))
                        {
                            client.Volume = volume;
                            Console.WriteLine($"Updated volume to {volume} for client {clientId}");
                        }
                        if (bool.TryParse(parsedBody["onoff"], out var onoff))
                        {
                            client.OnOff = onoff;
                            Console.WriteLine($"Updated volume to {volume} for client {clientId}");
                        }

                        await clientSettingsService.UpdateSettingsAsync(clientId, client.Channel, client.Volume);
                        Console.WriteLine($"Settings updated for client {clientId}");
                    }

                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in PUT request handling: {e}");
                    response.StatusCode = 500;
                }
            }
            else
            {
                Console.WriteLine($"Request method: {request.HttpMethod}");
                Console.WriteLine($"Request URL: {request.Url.AbsolutePath}");
                Console.WriteLine($"Request Content-Type: {request.Headers["Content-Type"]}");
                Console.WriteLine($"error {pathParts[3]} and {pathParts}");
            }
        }
        else if (request.HttpMethod == "GET" && request.Url.AbsolutePath.StartsWith("/api/client/"))
        {
            Console.WriteLine("Entering GET request handling");
            var pathParts = request.Url.AbsolutePath.Split('/');
            if (pathParts.Length == 4 && pathParts[3] == "settings")
            {
                var clientId = pathParts[2];
                var settings = await clientSettingsService.GetSettingsAsync(clientId);

                if (settings != null)
                {
                    var responseString = $"{{\"channel\":\"{settings.Channel}\",\"volume\":{settings.Volume}}}";
                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    Console.WriteLine($"Returned settings for client {clientId}: {responseString}");
                }
                else
                {
                    response.StatusCode = 404;
                    Console.WriteLine($"Settings not found for client {clientId}");
                }
            }
        }
        else
        {

            response.StatusCode = 404;
            Console.WriteLine("Request did not match any known API endpoints");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling request: {ex.Message}");
        response.StatusCode = 500;
    }
    finally
    {
        response.Close();
    }
}

}
