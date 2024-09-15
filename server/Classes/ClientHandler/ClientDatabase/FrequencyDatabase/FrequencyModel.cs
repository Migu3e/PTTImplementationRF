using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using server.ClientHandler.ClientDatabase;

namespace server.ClientHandler.FrequencyDatabase
{
    public class FrequencyModel
    {
        public FrequencyModel(ClientType clientType, double minFrequency, double maxFrequency)
        {
            ClientType = clientType;
            MinFrequency = minFrequency;
            MaxFrequency = maxFrequency;
        }

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public ClientType ClientType { get; set; }

        public double MinFrequency { get; set; }

        public double MaxFrequency { get; set; }
    }
}