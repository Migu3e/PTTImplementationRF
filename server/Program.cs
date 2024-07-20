using server.Classes;
using server.Classes.AudioHandler;
using server.Const;
using server.GridFS;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var clientManager = new ClientManager();
var gridFsManager = new GridFsManager(baseDirectory);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);
var server = new TcpServer(Constants.ServerPort, clientManager, receiveAudio);
await server.RunAsync();