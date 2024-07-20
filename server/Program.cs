using server.Classes;

var clientManager = new ClientManager();
var gridFsManager = new GridFsManager(Constants.MongoConnectionString, Constants.DatabaseName);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);
var server = new TcpServer(Constants.ServerPort, clientManager, receiveAudio);
await server.RunAsync();