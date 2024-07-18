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

            _ = Task.Run(() => ReceiveAudioFromServer(stream, _receiver));
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
                                await SendFullAudioToServer(stream, fullAudioMaker);
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
                    await TransmitAudioToServer(stream, sender);
                }

                await Task.Delay(10);
            }
        }

        private async Task TransmitAudioToServer(NetworkStream stream, ISender sender)
        {
            byte[] buffer = new byte[Sender.CHUNK_SIZE];
            if (sender.IsDataAvailable())
            {
                int bytesRead = sender.ReadAudio(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
                    await stream.WriteAsync(header, 0, header.Length);

                    byte[] lengthBytes = BitConverter.GetBytes(bytesRead);
                    await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

                    await stream.WriteAsync(buffer, 0, bytesRead);
                }
            }
        }

        private async Task SendFullAudioToServer(NetworkStream stream, IFullAudioMaker fullAudioMaker)
        {
            byte[] fullAudio = fullAudioMaker.GetFullAudioData();
            if (fullAudio.Length == 0)
            {
                Console.WriteLine(Constants.NoAudioDataMessage);
                return;
            }

            byte[] header = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            await stream.WriteAsync(header, 0, header.Length);

            byte[] lengthBytes = BitConverter.GetBytes(fullAudio.Length);
            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

            await stream.WriteAsync(fullAudio, 0, fullAudio.Length);

            Console.WriteLine($"Sent full audio ({fullAudio.Length} bytes) to server.");
        }

        private async Task ReceiveAudioFromServer(NetworkStream stream, IReceiver receiver)
        {
            byte[] buffer = new byte[4096];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        receiver.PlayAudio(buffer, 0, bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Constants.ErrorMessage} {ex.Message}");
                    break;
                }
            }
        }
    }
}
