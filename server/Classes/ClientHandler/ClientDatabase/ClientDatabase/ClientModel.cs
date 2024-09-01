using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace server.ClientHandler.ClientDatabase
{
    public class ClientModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ClientID { get; set; }
        public string Password { get; set; }
        public ClientType Type { get; set; }
    }

    public enum ClientType
    {
        Navy = 1,
        Land = 2,
        Air = 3,
        Magav = 4
    }
}