using MongoDB.Driver;
using server.ClientHandler;
using server.Enums;

namespace server.Classes.ClientHandler
{
    public class ClientSettingsService
    {
        private readonly IMongoCollection<ClientSettings> _settings;

        public ClientSettingsService(IMongoDatabase database)
        {
            _settings = database.GetCollection<ClientSettings>("ClientSettings");
        }

        public async Task UpdateSettingsAsync(string clientId, FrequencyChannel channel, int volume)
        {
            var filter = Builders<ClientSettings>.Filter.Eq(s => s.ClientId, clientId);
            var update = Builders<ClientSettings>.Update
                .Set(s => s.Channel, channel)
                .Set(s => s.Volume, volume);

            await _settings.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        public async Task<ClientSettings> GetSettingsAsync(string clientId)
        {
            return await _settings.Find(s => s.ClientId == clientId).FirstOrDefaultAsync();
        }
    }
}