using GridFs;
using MongoDB.Driver;
using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Classes.Logging;
using server.Classes.WebSocket;
using server.Const;


var clientManager = new ClientManager();

var mongoClient = new MongoClient(Constants.MongoConnectionString);
var databaseAudio = mongoClient.GetDatabase(Constants.DatabaseNameAudio);
var databaseLogs = mongoClient.GetDatabase(Constants.DatabaseNameLogs);


var gridFsManager = new GridFsManager(databaseAudio);
var loggingService = new LoggingService(databaseLogs);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager, loggingService);

var webSocketServer = new WebSocketServer(Constants.WebSocketServerPort, clientManager, transmitAudio, receiveAudio, loggingService);


var serverOptions = new ServerOptions(clientManager, webSocketServer);
var webSocketServerTask = webSocketServer.StartAsync();

Console.WriteLine(Constants.StartedConnection);
Console.WriteLine($"{Constants.ServerConnectionPoint} {webSocketServer.GetServerAddress()}");


bool isRunning = true;
while (isRunning)
{
    isRunning = await serverOptions.HandleInput();
    await Task.Delay(100);
}

Console.WriteLine(Constants.StoppedConnection);
await webSocketServerTask;