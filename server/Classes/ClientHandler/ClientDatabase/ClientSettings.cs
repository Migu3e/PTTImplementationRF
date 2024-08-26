using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using server.Enums;

namespace server.ClientHandler
{
    public class ClientSettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ClientId { get; }

        public double Frequency { get; }
        public int Volume { get; }

        public ClientSettings(string clientId, double frequency, int volume)
        {
            ClientId = clientId;
            Frequency = frequency;
            Volume = volume;
        }
    }
}