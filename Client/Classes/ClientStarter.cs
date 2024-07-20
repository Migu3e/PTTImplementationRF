using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Client.Const;
using Client.Interfaces;

namespace Client.Classes
{
    public class ClientStarter : IClientStarter
    {
        private readonly IFullAudioMaker _fullAudioMaker;
        private readonly IReceiver _receiver;
        private readonly ISender _sender;

        public ClientStarter(IFullAudioMaker fullAudioMaker, IReceiver receiver, ISender sender)
        {
            _fullAudioMaker = fullAudioMaker;
            _receiver = receiver;
            _sender = sender;
        }

        public async Task StartAsync()
        {
            using TcpClient tcpClient = new TcpClient();
            if (!await ConnectToServer(tcpClient))
                return;

            using NetworkStream stream = tcpClient.GetStream();

            _ = Task.Run(() => _receiver.ReceiveAudioFromServer(stream, _receiver));
            await HandleUserInput(stream, _sender, _fullAudioMaker);

            _sender.Stop();
            _receiver.Stop();
            tcpClient.Close();
            Console.WriteLine(Constants.DisconnectedMessage);
            Console.WriteLine(Constants.ProgramExitedMessage);
        }

        private async Task<bool> ConnectToServer(TcpClient tcpClient)
        {
            try
            {
                await tcpClient.ConnectAsync(Constants.ServerIP, Constants.ServerPort);
                Console.WriteLine(Constants.ConnectedMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Constants.FailedToConnectMessage} {ex.Message}");
                return false;
            }
        }

        private async Task HandleUserInput(NetworkStream stream, ISender sender, IFullAudioMaker fullAudioMaker)
        {
            bool isRunning = true;
            bool isTransmitting = false;

            Console.WriteLine(Constants.PressMessage);

            while (isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.T:
                            isTransmitting = !isTransmitting;
                            if (isTransmitting)
                            {
                                sender.Start();
                                fullAudioMaker.StartRecording();
                                Console.WriteLine(Constants.TransmissionStartedMessage);
                            }
                            else
                            {
                                await Task.Delay(50);
                                sender.Stop();
                                fullAudioMaker.StopRecording();
                                await _sender.SendFullAudioToServer(stream, fullAudioMaker);
                                Console.WriteLine(Constants.TransmissionStoppedMessage);
                            }
                            break;
                        case ConsoleKey.Q:
                            isRunning = false;
                            break;
                    }
                }

                if (isTransmitting)
                {
                    await _sender.TransmitAudioToServer(stream, sender);
                }

                await Task.Delay(10);
            }
        }

        
    }
}
