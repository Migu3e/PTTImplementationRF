using System;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        RadioChannel channel = new RadioChannel();
        Sender sender = new Sender(channel);
        Receiver receiver = new Receiver(channel);

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

        bool isTransmitting = false;
        bool isRunning = true;

        Console.WriteLine("Press 'T' to start/stop transmission. Press 'Q' to quit.");

        while (isRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.T:
                        if (isTransmitting)
                        {
                            sender.Stop();
                            receiver.Stop();
                            isTransmitting = false;
                            Console.WriteLine("Transmission stopped.");
                        }
                        else
                        {
                            sender.Start();
                            receiver.Start();
                            isTransmitting = true;
                            Console.WriteLine("Transmission started.");
                        }
                        break;

                    case ConsoleKey.Q:
                        isRunning = false;
                        break;
                }
            }

            await Task.Delay(100);  // Small delay to prevent CPU overuse
        }

        if (isTransmitting)
        {
            sender.Stop();
            receiver.Stop();
        }

        // Close the TCP connection
        tcpClient.Close();
        Console.WriteLine("Disconnected from TCP server");

        Console.WriteLine("Program exited.");
    }
}