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
        private byte currentChannel = 1;



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
            {
                Console.WriteLine(Constants.FailedToConnectMessage);
                await Task.Delay(5000);
            }

            using NetworkStream stream = tcpClient.GetStream();

            var receiveTask = Task.Run(() => _receiver.ReceiveAudioFromServer(stream, _receiver));
            var handleInputTask = HandleUserInput(stream, _sender, _fullAudioMaker);

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
            Console.WriteLine(Constants.DisconnectedMessage);

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
                        case ConsoleKey.C:
                            ChangeChannel();
                            break;
                        
                    }
                }

                if (isTransmitting)
                {
                    await _sender.TransmitAudioToServer(stream, sender,currentChannel);
                }

                await Task.Delay(10);
            }

        }
        public void ChangeChannel()
        {
            Console.Write("Enter channel number (1-11): ");
            if (byte.TryParse(Console.ReadLine(), out byte newChannel) && newChannel >= 1 && newChannel <= 11)
            {
                currentChannel = newChannel;
                Console.WriteLine($"Switched to channel {currentChannel}");
            }
            else
            {
                Console.WriteLine("Invalid channel number. Please enter a number between 1 and 11.");
            }
            
        }

        
    }
    
}
