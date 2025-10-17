using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Models
{
    public class ProfileDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonElement("username")]
        public string Username { get; set; } = "";
        
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = "";
        
        [BsonElement("salt")]
        public string Salt { get; set; } = "";
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("lastPlayed")]
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
        
        [BsonElement("totalGamesPlayed")]
        public int TotalGamesPlayed { get; set; } = 0;
    }
}