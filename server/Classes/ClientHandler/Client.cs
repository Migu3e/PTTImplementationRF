using System.Net.WebSockets;

namespace server.Classes.ClientHandler
{
    public class Client
    {
        public string Id { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public double Frequency { get; set; }
        public bool OnOff { get; set; }
        public int Volume { get; set; }

        public Client(string id, System.Net.WebSockets.WebSocket webSocket)
        {
            Id = id;
            WebSocket = webSocket;
            Frequency = 30.0000;
            Volume = 50;
            OnOff = true;
        }

     }
}