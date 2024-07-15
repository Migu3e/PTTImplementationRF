using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

class Program
{
    static bool isTransmitting = false;
    static bool isRunning = true;

    static async Task Main(string[] args)
    {
        Sender sender = new Sender();
        Receiver receiver = new Receiver();

        // Connect to the TCP server
        TcpClient tcpClient = new TcpClient();
        try
        {
            await tcpClient.ConnectAsync("localhost", 8080); // Replace "localhost" with the server's IP if needed
            Console.WriteLine("Connected to TCP server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to TCP server: {ex.Message}");
            return;
        }

        NetworkStream stream = tcpClient.GetStream();

        Console.WriteLine("Press 'T' to start/stop transmission. Press 'Q' to quit.");

        _ = Task.Run(async () => await ReceiveAudioFromServer(stream, receiver));
        _ = Task.Run(() => MonitorKeyPresses(sender));

        while (isRunning)
        {
            if (isTransmitting)
            {
                await TransmitAudioToServer(stream, sender);
            }
            await Task.Delay(10);
        }

        // Close the TCP connection
        tcpClient.Close();
        Console.WriteLine("Disconnected from TCP server");

        Console.WriteLine("Program exited.");
    }

    static void MonitorKeyPresses(Sender sender)
    {
        while (isRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.T)
                {
                    isTransmitting = !isTransmitting;
                    if (isTransmitting)
                    {
                        sender.Start();
                        Console.WriteLine("Transmission started.");
                    }
                    else
                    {
                        sender.Stop();
                        Console.WriteLine("Transmission stopped.");
                    }
                }
                else if (key == ConsoleKey.Q)
                {
                    isRunning = false;
                    if (isTransmitting)
                    {
                        isTransmitting = false;
                        sender.Stop();
                    }
                }
            }
            Thread.Sleep(10);
        }
    }

    static async Task TransmitAudioToServer(NetworkStream stream, Sender sender)
    {
        byte[] buffer = new byte[4096]; // Increased buffer size
        int bytesRead = sender.ReadAudio(buffer, 0, buffer.Length);
        if (bytesRead > 0)
        {
            await stream.WriteAsync(buffer, 0, bytesRead);
        }
    }

    static async Task ReceiveAudioFromServer(NetworkStream stream, Receiver receiver)
    {
        byte[] buffer = new byte[4096]; // Match the transmit buffer size
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
            }
        }
    }
}