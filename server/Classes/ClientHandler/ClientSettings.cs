using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using server.Enums;

namespace server.ClientHandler
{
    public class ClientSettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ClientId { get; set; }
        public FrequencyChannel Channel { get; set; }
        public int Volume { get; set; }
    }
}