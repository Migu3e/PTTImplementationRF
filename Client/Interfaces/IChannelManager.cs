namespace Client.Interfaces;

public interface IChannelManager
{
    void ChangeChannel();
    byte CurrentChannel();
}