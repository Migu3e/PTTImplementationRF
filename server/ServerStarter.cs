using System.Net;
using GridFs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.WebSocket;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.FrequencyDatabase;
using server.ClientHandler.VolumeDatabase;
using server.Const;

namespace server;

public class ServerStarter
{
    public async Task StartServer()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PTT Audio API", Version = "v1" });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PTT Audio API v1");
            c.RoutePrefix = string.Empty;
        });

        app.MapControllers();

        var mongoClient = new MongoClient(Constants.MongoConnectionString);
        var database = mongoClient.GetDatabase(Constants.DatabaseName);

        var clientManager = new ClientManager();
        var gridFsManager = new GridFsManager(database);
        var transmitAudio = new TransmitAudio(clientManager);
        var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

        var accountService = new AccountService(database);
        var channelService = new ChannelService(database);
        var volumeService = new VolumeService(database);
        var frequencyService = new FrequencyService(database);

        var webSocketServer = new WebSocketServer(Constants.WebSocketServerPort, clientManager, receiveAudio, database);
        var serverOptions = new ServerOptions(clientManager, webSocketServer);

        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5000/");
        httpListener.Start();

        Console.WriteLine(Constants.StartedConnection);
        Console.WriteLine($"{Constants.ServerConnectionPoint} http://localhost:5000");

        var httpRequestHandler = new HttpRequestHandler(clientManager, 
            accountService, channelService, 
            volumeService, frequencyService);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                var context = await httpListener.GetContextAsync();
                _ = httpRequestHandler.HandleRequestAsync(context);
            }
        });

        var webSocketServerTask = webSocketServer.StartAsync();

        Task.Run(() => app.Run("http://localhost:5001"));

        bool continueRunning = true;
        while (continueRunning)
        {
            continueRunning = await serverOptions.HandleInput();
            await Task.Delay(100);
        }

        await webSocketServer.StopAsync();
        httpListener.Stop();
        Console.WriteLine(Constants.StoppedConnection);

    }
}