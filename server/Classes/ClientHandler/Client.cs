using System.Net.WebSockets;

namespace server.Classes.ClientHandler
{
    public class Client
    {
        public string Id { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public double Frequency { get; set; }

        public Client(string id, System.Net.WebSockets.WebSocket webSocket, double frequency = 29.988)
        {
            Id = id;
            WebSocket = webSocket;
            Frequency = frequency;
        }

     }
}