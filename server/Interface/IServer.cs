namespace server.Interface;

public interface IServer
{
    Task StartAsync();
    Task StopAsync();
}