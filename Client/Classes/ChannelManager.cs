using Client.Const;
using Client.Interfaces;

namespace Client.Classes;

public class ChannelManager : IChannelManager
{
    private byte currentChannel = 1;

    public byte CurrentChannel()
    {
        return currentChannel;
    }
    public void ChangeChannel()
    {
        Console.Write(ConstString.EnterChannel);
        if (byte.TryParse(Console.ReadLine(), out byte newChannel) && newChannel >= 1 && newChannel <= 11)
        {
            currentChannel = newChannel;
            Console.WriteLine(ConstString.SwitchedChannel + currentChannel);
        }
        else
        {
            Console.WriteLine(ConstString.InvalidChannelNumber);
        }
    }
}