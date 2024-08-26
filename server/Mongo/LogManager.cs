using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Classes.Logging
{
    public class LoggingService
    {
        private readonly IMongoCollection<LogDocument> _clientLogs;
        private readonly IMongoCollection<LogDocument> _serverLogs;

        public LoggingService(IMongoDatabase database)
        {
            _clientLogs = database.GetCollection<LogDocument>("ClientLogs");
            _serverLogs = database.GetCollection<LogDocument>("ServerLogs");
        }

        public async Task LogClientAction(string clientId, string action)
        {
            var filter = Builders<LogDocument>.Filter.Eq(x => x.Id, $"data_of_{clientId}");
            var update = Builders<LogDocument>.Update
                .Push(x => x.Actions, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {action}");

            await _clientLogs.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }

        public async Task LogServerAction(string action)
        {
            var filter = Builders<LogDocument>.Filter.Eq(x => x.Id, "Server");
            var update = Builders<LogDocument>.Update
                .Push(x => x.Actions, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {action}");

            await _serverLogs.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
    }

    public class LogDocument
    {
        [BsonId]
        public string Id { get; set; }

        public List<string> Actions { get; set; } = new List<string>();
    }
}