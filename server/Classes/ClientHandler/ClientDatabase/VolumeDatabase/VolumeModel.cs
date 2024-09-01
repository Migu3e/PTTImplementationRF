using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace server.ClientHandler.VolumeDatabase
{
    public class VolumeModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ClientID { get; set; }

        public int LastVolume { get; set; }
    }
}