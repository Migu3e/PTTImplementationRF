using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        TcpServer server = new TcpServer(8080); // Choose an appropriate port

        Console.WriteLine("Starting TCP Server...");
        Task serverTask = server.StartAsync();

        Console.WriteLine("Press 'Q' to quit, 'L' to list connected clients.");
        Console.Out.Flush(); // Force the console to display the message immediately

        bool isRunning = true;
        while (isRunning)
        {
            while (!Console.KeyAvailable && isRunning)
            {
                // Check if the server task has completed (which shouldn't happen unless there's an error)
                if (serverTask.IsCompleted)
                {
                    Console.WriteLine("Server task completed unexpectedly. Exiting...");
                    isRunning = false;
                    break;
                }

                await Task.Delay(100);  // Small delay to prevent CPU overuse
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Q:
                        isRunning = false;
                        break;
                    case ConsoleKey.L:
                        server.ListConnectedClients();
                        Console.Out.Flush(); // Flush after listing clients
                        break;
                    default:
                        Console.WriteLine($"Unrecognized key: {key}. Press 'Q' to quit or 'L' to list clients.");
                        Console.Out.Flush();
                        break;
                }
            }
        }

        Console.WriteLine("Stopping server...");
        await server.StopAsync();
        Console.WriteLine("Server stopped. Press any key to exit.");
        Console.ReadKey();
    }
}