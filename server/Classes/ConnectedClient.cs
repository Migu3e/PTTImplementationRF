using System.Net.Sockets;

public class ConnectedClient
{
    public string Id { get; }
    public TcpClient TcpClient { get; }

    public ConnectedClient(string id, TcpClient tcpClient)
    {
        Id = id;
        TcpClient = tcpClient;
    }
}