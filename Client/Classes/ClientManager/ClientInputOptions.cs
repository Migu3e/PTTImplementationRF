using System.Net.Sockets;
using Client.Const;
using Client.Interfaces;

namespace Client.Classes.ClientManager
{
    public class ClientInputOptions : IClientInputOptions
    {
        private readonly IChannelManager _channelManager;
        private readonly ITransmissionManager _transmissionManager;
        private bool _shouldExit;

        public ClientInputOptions(IChannelManager channelManager, ITransmissionManager transmissionManager)
        {
            _channelManager = channelManager;
            _transmissionManager = transmissionManager;
            _shouldExit = false;
        }

        public bool ShouldExit()
        {
            return _shouldExit;
        }
        public async Task HandleInput(ConsoleKey key, NetworkStream stream)
        {
            await (key switch
            {
                ConsoleKey.T => _transmissionManager.ToggleTransmission(stream),
                ConsoleKey.Q => HandleExit(),
                ConsoleKey.C => Task.Run(() => _channelManager.ChangeChannel()),
                _ => Task.Run(() => Console.WriteLine(ConstString.InvalidKeyMessage))
            });
        }
        
        private Task HandleExit()
        {
            _shouldExit = true;
            Console.WriteLine(ConstString.ExitMessage);
            return Task.CompletedTask;
        }
    }
}