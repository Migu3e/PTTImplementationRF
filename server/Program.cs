﻿using System.Net;
using GridFs;
using MongoDB.Driver;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.WebSocket;
using server.Const;
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.VolumeDatabase;
using server.ClientHandler.FrequencyDatabase;
using server.Classes;

var mongoClient = new MongoClient(Constants.MongoConnectionString);
var database = mongoClient.GetDatabase(Constants.DatabaseName);

var clientManager = new ClientManager();
var clientSettingsService = new ClientSettingsService(database);
var gridFsManager = new GridFsManager(database);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

var accountService = new AccountService(database);
var channelService = new ChannelService(database);
var volumeService = new VolumeService(database);
var frequencyService = new FrequencyService(database);

var webSocketServer = new WebSocketServer(Constants.WebSocketServerPort, clientManager, transmitAudio, receiveAudio,database);
var serverOptions = new ServerOptions(clientManager, webSocketServer);

// Start HTTP listener
var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:5000/");
httpListener.Start();

Console.WriteLine(Constants.StartedConnection);
Console.WriteLine($"{Constants.ServerConnectionPoint} http://localhost:5000");

var httpRequestHandler = new HttpRequestHandler(clientManager, clientSettingsService, 
    accountService, channelService, 
    volumeService, frequencyService);

// Handle HTTP requests
_ = Task.Run(async () =>
{
    while (true)
    {
        var context = await httpListener.GetContextAsync();
        _ = httpRequestHandler.HandleRequestAsync(context);
    }
});

// Start WebSocket server
var webSocketServerTask = webSocketServer.StartAsync();

// Main program loop
bool continueRunning = true;
while (continueRunning)
{
    continueRunning = await serverOptions.HandleInput();
    await Task.Delay(100); // Small delay to prevent CPU overuse
}

// Cleanup
await webSocketServer.StopAsync();
httpListener.Stop();
Console.WriteLine(Constants.StoppedConnection);