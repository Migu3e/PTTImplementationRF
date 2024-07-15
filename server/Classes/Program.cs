using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string mongoConnectionString = "mongodb://localhost:27017/?directConnection=true"; // Replace with your MongoDB connection string
        string databaseName = "PTTAudioDB"; // Replace with your desired database name
        TcpServer server = new TcpServer(8080, mongoConnectionString, databaseName);

        Console.WriteLine("Starting TCP Server...");
        Task serverTask = server.StartAsync();

        Console.WriteLine("Press 'Q' to quit, 'L' to list connected clients.");
        Console.Out.Flush();

        bool isRunning = true;
        while (isRunning)
        {
            while (!Console.KeyAvailable && isRunning)
            {
                if (serverTask.IsCompleted)
                {
                    Console.WriteLine("Server task completed unexpectedly. Exiting...");
                    isRunning = false;
                    break;
                }

                await Task.Delay(100);
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
                        Console.Out.Flush();
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