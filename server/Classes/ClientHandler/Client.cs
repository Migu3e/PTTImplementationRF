using System.Net.Sockets;
using System.Net.WebSockets;

namespace server.Classes.ClientHandler
{
    public class Client
    {
        public string Id { get; }
        public TcpClient? TcpClient { get; }
        public System.Net.WebSockets.WebSocket? WebSocket { get; }
        public int Channel { get; set; }

        //  TCP clients
        public Client(string id, TcpClient tcpClient, int channel = 1)
        {
            Id = id;
            TcpClient = tcpClient;
            WebSocket = null;
            Channel = channel;
        }

        //  WebSocket clients
        public Client(string id, System.Net.WebSockets.WebSocket webSocket, int channel = 1)
        {
            Id = id;
            TcpClient = null;
            WebSocket = webSocket;
            Channel = channel;
        }

        public bool IsTcpClient => TcpClient != null;
        public bool IsWebSocketClient => WebSocket != null;
    }
}