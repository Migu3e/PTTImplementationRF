using System.Net.WebSockets;

namespace server.Classes.ClientHandler
{
    public class Client
    {
        public string Id { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public int Channel { get; set; }

        public Client(string id, System.Net.WebSockets.WebSocket webSocket, int channel = 1)
        {
            Id = id;
            WebSocket = webSocket;
            Channel = channel;
        }

     }
}