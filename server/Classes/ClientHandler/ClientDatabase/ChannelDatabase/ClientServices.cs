using MongoDB.Driver;

namespace server.ClientHandler.ChannelDatabase
{
    public class ChannelService
    {
        private readonly IMongoCollection<ChannelModel> _channels;

        public ChannelService(IMongoDatabase database)
        {
            _channels = database.GetCollection<ChannelModel>("Channels");
        }

        public async Task<ChannelModel> GetChannelInfo(string clientId)
        {
            return await _channels.Find(c => c.ClientID == clientId).FirstOrDefaultAsync();
        }

        public async Task UpdateChannelInfo(string clientId, int channel, double frequency)
        {
            var update = Builders<ChannelModel>.Update
                .Set(c => c.Channel, channel)
                .Set(c => c.Frequency, frequency);
            await _channels.UpdateOneAsync(c => c.ClientID == clientId, update);
        }

        public async Task AddChannelInfo(string clientId, int channel, double frequency)
        {
            var newChannel = new ChannelModel
            {
                ClientID = clientId,
                Channel = channel,
                Frequency = frequency
            };
            await _channels.InsertOneAsync(newChannel);
        }

    }
}