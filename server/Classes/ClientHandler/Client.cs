using System.Net.Sockets;

public class Client
{
    public string Id { get; }
    public TcpClient TcpClient { get; }
    public int Channel { get; set; }

    public Client(string id, TcpClient tcpClient, int channel = 1)
    {
        Id = id;
        TcpClient = tcpClient;
        Channel = channel;
    }
}