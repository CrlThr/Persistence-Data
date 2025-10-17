using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Models
{
    public class EncryptedSaveDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        [BsonElement("username")]
        public string Username { get; set; } = "";
        
        [BsonElement("ciphertext")]
        public string Ciphertext { get; set; } = "";
        
        [BsonElement("nonce")]
        public string Nonce { get; set; } = "";
        
        [BsonElement("tag")]
        public string Tag { get; set; } = "";
        
        [BsonElement("salt")]
        public string Salt { get; set; } = "";
        
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("highScore")]
        public int HighScore { get; set; } = 0; // For quick leaderboard queries
    }
}