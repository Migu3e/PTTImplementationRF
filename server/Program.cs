using System;
using System.Threading.Tasks;
using GridFs;
using MongoDB.Driver;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.WebSocket;
using server.Const;
using server.Interface;



var clientManager = new ClientManager();

var mongoClient = new MongoClient(Constants.MongoConnectionString);
var database = mongoClient.GetDatabase(Constants.DatabaseName);

var gridFsManager = new GridFsManager(database);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);
var webSocketServer = new WebSocketServer($"http://localhost:{Constants.WebSocketServerPort}/", clientManager, transmitAudio, receiveAudio);
var serverOptions = new ServerOptions(clientManager, webSocketServer);
var webSocketServerTask = webSocketServer.StartAsync();

Console.WriteLine(Constants.StartedConnection);

bool isRunning = true;
while (isRunning)
{
    isRunning = await serverOptions.HandleInput();
    await Task.Delay(100);
}

Console.WriteLine(Constants.StoppedConnection);
await webSocketServerTask;