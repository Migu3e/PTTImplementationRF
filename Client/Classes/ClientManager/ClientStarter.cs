using System.Net.Sockets;
using Client.Classes.AudioHandler;
using Client.Const;
using Client.Interfaces;

namespace Client.Classes.ClientManager
{
    public class ClientStarter : IClientStarter
    {
        private readonly IFullAudioMaker _fullAudioMaker;
        private readonly IReceiver _receiver;
        private readonly ISender _sender;
        private readonly IChannelManager _channelManager;
        private readonly ITransmissionManager _transmissionManager;
        private readonly IClientInputOptions _inputHandler;

        public ClientStarter(IFullAudioMaker fullAudioMaker, IReceiver receiver, ISender sender)
        {
            _fullAudioMaker = fullAudioMaker;
            _receiver = receiver;
            _sender = sender;
            _channelManager = new ChannelManager();
            _transmissionManager = new TransmissionManager(sender, fullAudioMaker);
            _inputHandler = new ClientInputOptions(_channelManager, _transmissionManager);
        }

        public async Task StartAsync()
        {
            using TcpClient tcpClient = new TcpClient();
            if (!await ConnectToServer(tcpClient))
            {
                Console.WriteLine(ConstString.FailedToConnectMessage);
                await Task.Delay(5000);
                return;
            }

            using NetworkStream stream = tcpClient.GetStream();

            var receiveTask = Task.Run(() => _receiver.ReceiveAudioFromServer(stream, _receiver));
            var handleInputTask = HandleUserInput(stream);

            try
            {
                await Task.WhenAny(receiveTask, handleInputTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            _sender.Stop();
            _receiver.Stop();
            tcpClient.Close();
            Console.WriteLine(ConstString.DisconnectedMessage);
        }

        private async Task<bool> ConnectToServer(TcpClient tcpClient)
        {
            try
            {
                await tcpClient.ConnectAsync(ConstString.ServerIP, ConstString.ServerPort);
                Console.WriteLine(ConstString.ConnectedMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ConstString.FailedToConnectMessage} {ex.Message}");
                return false;
            }
        }

        private async Task HandleUserInput(NetworkStream stream)
        {
            Console.WriteLine(ConstString.PressMessage);

            while (!_inputHandler.ShouldExit())
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    await _inputHandler.HandleInput(key, stream);
                }

                await _transmissionManager.TransmitAudio(stream, _channelManager.CurrentChannel());

                await Task.Delay(10);
            }
        }
    }
}