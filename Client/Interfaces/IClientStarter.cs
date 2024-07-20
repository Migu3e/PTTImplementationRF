namespace Client.Interfaces
{
    public interface IClientStarter
    {
        Task StartAsync();
        void ChangeChannel();
    }
}