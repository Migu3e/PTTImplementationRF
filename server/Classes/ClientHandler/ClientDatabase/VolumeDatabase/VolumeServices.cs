using MongoDB.Driver;
using server.ClientHandler.VolumeDatabase;

namespace server.ClientHandler.VolumeDatabase
{
    public class VolumeService
    {
        private readonly IMongoCollection<VolumeModel> _volumes;

        public VolumeService(IMongoDatabase database)
        {
            _volumes = database.GetCollection<VolumeModel>("Volumes");
        }

        public async Task<int> GetLastVolume(string clientId)
        {
            var volume = await _volumes.Find(v => v.ClientID == clientId).FirstOrDefaultAsync();
            return volume?.LastVolume ?? 50; // Default to 50 if not set
        }

        public async Task UpdateVolume(string clientId, int volume)
        {
            var update = Builders<VolumeModel>.Update.Set(v => v.LastVolume, volume);
            await _volumes.UpdateOneAsync(v => v.ClientID == clientId, update, new UpdateOptions { IsUpsert = true });
        }
    }
}