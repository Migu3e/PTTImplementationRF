using server.Classes;
using server.Classes.AudioHandler;
using server.Classes.ClientHandler;
using server.Const;
using server.GridFS;
using server.Interface;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var clientManager = new ClientManager();
var gridFsManager = new GridFsManager(baseDirectory);
var transmitAudio = new TransmitAudio(clientManager);
var receiveAudio = new ReceiveAudio(transmitAudio, gridFsManager);

IClientHandler clientHandler = new ClientHandler(clientManager, receiveAudio);

var server = new TcpServer(Constants.ServerPort, clientManager, receiveAudio, clientHandler);
await server.RunAsync();