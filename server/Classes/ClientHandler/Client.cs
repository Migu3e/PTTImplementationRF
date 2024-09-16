
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.VolumeDatabase;
using server.ClientHandler.FrequencyDatabase;
using MongoDB.Driver;

namespace server.Classes.ClientHandler
{
    public class Client
    {
        public string Id { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public double Frequency { get; set; }
        public int Volume { get; set; }
        public bool OnOff { get; set; }
        public ClientType Type { get; set; }
        public int Channel { get; set; }
        public double MinFrequency { get; set; }
        public double MaxFrequency { get; set; }

        private readonly AccountService _accountService;
        private readonly ChannelService _channelService;
        private readonly VolumeService _volumeService;
        private readonly FrequencyService _frequencyService;

        public Client(string id, System.Net.WebSockets.WebSocket webSocket, IMongoDatabase database)
        {
            Id = id;
            WebSocket = webSocket;

            _accountService = new AccountService(database);
            _channelService = new ChannelService(database);
            _volumeService = new VolumeService(database);
            _frequencyService = new FrequencyService(database);

            InitializeClientDataAsync().Wait();
        }

        private async Task InitializeClientDataAsync()
        {
            var account = await _accountService.GetAccount(Id);
            if (account == null)
            {
                throw new Exception($"Account not found for client ID: {Id}");
            }

            Type = account.Type;

            var channelInfo = await _channelService.GetChannelInfo(Id);
            Channel = channelInfo?.Channel ?? 1;
            Frequency = channelInfo?.Frequency ?? 30.0000;

            Volume = await _volumeService.GetLastVolume(Id);

            var frequencyRange = await _frequencyService.GetFrequencyRange(Type);
            MinFrequency = frequencyRange?.MinFrequency ?? 30.0000;
            MaxFrequency = frequencyRange?.MaxFrequency ?? 88.0000;

            OnOff = false; // Default to off when initializing
        }
    }
}