using MongoDB.Driver;
using server.ClientHandler.ClientDatabase;

namespace server.ClientHandler.FrequencyDatabase
{
    public class FrequencyService
    {
        private readonly IMongoCollection<FrequencyModel> _frequencies;

        public FrequencyService(IMongoDatabase database)
        {
            _frequencies = database.GetCollection<FrequencyModel>("Frequencies");
        }

        public async Task<FrequencyModel> GetFrequencyRange(ClientType clientType)
        {
            return await _frequencies
                .Find(f => f.ClientType == clientType)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateFrequencyRange(ClientType clientType, double minFrequency, double maxFrequency)
        {
            var update = Builders<FrequencyModel>.Update
                .Set(f => f.MinFrequency, minFrequency)
                .Set(f => f.MaxFrequency, maxFrequency);

            await _frequencies.UpdateOneAsync(
                f => f.ClientType == clientType,
                update,
                new UpdateOptions { IsUpsert = true }
            );
        }

        public async Task AddFrequencyRange(ClientType clientType, double minFrequency, double maxFrequency)
        {
            var newModel = new FrequencyModel(clientType, minFrequency, maxFrequency);
            await _frequencies.InsertOneAsync(newModel);
        }
    }
}