using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace server.ClientHandler.ChannelDatabase
{
    public class ChannelModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ClientID { get; set; }

        public int Channel { get; set; }

        public double Frequency { get; set; }
    }
}