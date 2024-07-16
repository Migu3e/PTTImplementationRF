using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Client.Classes;

namespace Client
{
    class Program
    {
        static bool isRunning = true;
        static bool isTransmitting = false;

        static async Task Main(string[] args)
        {
            FullAudioMaker fullAudioMaker = new FullAudioMaker();
            Receiver receiver = new Receiver();
            Sender sender = new Sender();

            // Connect to the TCP server
            TcpClient tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync("localhost", 8080);
                Console.WriteLine("Connected to TCP server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to TCP server: {ex.Message}");
                return;
            }

            NetworkStream stream = tcpClient.GetStream();

            // Start a task to receive audio from the server
            _ = Task.Run(() => ReceiveAudioFromServer(stream, receiver));

            Console.WriteLine("Press 'T' to start/stop transmission and recording, 'Q' to quit.");

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
                                Console.WriteLine("Transmission and recording started. Press 'T' to stop.");
                            }
                            else
                            {
                                sender.Stop();
                                fullAudioMaker.StopRecording();
                                await SendFullAudioToServer(stream, fullAudioMaker);
                                Console.WriteLine("Transmission and recording stopped. Full audio sent to server.");
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

            // Close the TCP connection
            sender.Stop();
            receiver.Stop();
            tcpClient.Close();
            Console.WriteLine("Disconnected from TCP server");

            Console.WriteLine("Program exited.");
        }

        static async Task TransmitAudioToServer(NetworkStream stream, Sender sender)
        {
            byte[] buffer = new byte[Sender.CHUNK_SIZE];
            if (sender.IsDataAvailable())
            {
                int bytesRead = sender.ReadAudio(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    // Send a header to indicate real-time audio transmission
                    byte[] header = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
                    await stream.WriteAsync(header, 0, header.Length);

                    // Send the length of the audio data
                    byte[] lengthBytes = BitConverter.GetBytes(bytesRead);
                    await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

                    // Send the audio data
                    await stream.WriteAsync(buffer, 0, bytesRead);
                }
            }
        }

        static async Task SendFullAudioToServer(NetworkStream stream, FullAudioMaker fullAudioMaker)
        {
            byte[] fullAudio = fullAudioMaker.GetFullAudioData();
            
            if (fullAudio.Length == 0)
            {
                Console.WriteLine("No audio data to send.");
                return;
            }

            // Send a header to indicate full audio transmission
            byte[] header = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            await stream.WriteAsync(header, 0, header.Length);
            
            // Send the length of the audio data
            byte[] lengthBytes = BitConverter.GetBytes(fullAudio.Length);
            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            
            // Send the full audio data
            await stream.WriteAsync(fullAudio, 0, fullAudio.Length);
            
            Console.WriteLine($"Sent full audio ({fullAudio.Length} bytes) to server.");
        }

        static async Task ReceiveAudioFromServer(NetworkStream stream, Receiver receiver)
        {
            byte[] buffer = new byte[4096];
            while (isRunning)
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
                    Console.WriteLine($"Error receiving audio: {ex.Message}");
                    if (!isRunning) break;
                }
            }
        }
    }
}