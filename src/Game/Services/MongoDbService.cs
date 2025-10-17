using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using Game.Models;

namespace Game.Services
{
    public class MongoDbService
    {
        private readonly MongoContext _context;
        
        public MongoDbService(MongoContext context)
        {
            _context = context;
            CreateIndexesAsync().Wait();
        }
        
        private async Task CreateIndexesAsync()
        {
            // Create unique index on username for profiles
            var usernameIndex = Builders<ProfileDoc>.IndexKeys.Ascending(x => x.Username);
            await _context.Profiles.Indexes.CreateOneAsync(
                new CreateIndexModel<ProfileDoc>(usernameIndex, 
                new CreateIndexOptions { Unique = true }));
            
            // Create index on username for saves
            var saveUsernameIndex = Builders<EncryptedSaveDoc>.IndexKeys.Ascending(x => x.Username);
            await _context.Saves.Indexes.CreateOneAsync(
                new CreateIndexModel<EncryptedSaveDoc>(saveUsernameIndex));
            
            // Create index on high score for leaderboards
            var highScoreIndex = Builders<EncryptedSaveDoc>.IndexKeys.Descending(x => x.HighScore);
            await _context.Saves.Indexes.CreateOneAsync(
                new CreateIndexModel<EncryptedSaveDoc>(highScoreIndex));
        }
        
        public async Task<ProfileDoc?> GetProfileByUsernameAsync(string username)
        {
            return await _context.Profiles
                .Find(x => x.Username == username)
                .FirstOrDefaultAsync();
        }
        
        public async Task<ProfileDoc> CreateProfileAsync(ProfileDoc profile)
        {
            await _context.Profiles.InsertOneAsync(profile);
            return profile;
        }
        
        public async Task<ProfileDoc> UpdateProfileAsync(ProfileDoc profile)
        {
            var filter = Builders<ProfileDoc>.Filter.Eq(x => x.Id, profile.Id);
            await _context.Profiles.ReplaceOneAsync(filter, profile);
            return profile;
        }
        
        public async Task<EncryptedSaveDoc?> GetSaveByUsernameAsync(string username)
        {
            return await _context.Saves
                .Find(x => x.Username == username)
                .FirstOrDefaultAsync();
        }
        
        public async Task<EncryptedSaveDoc> SavePlayerDataAsync(string username, PlayerData playerData, string password)
        {
            // Serialize and encrypt the player data
            string json = JsonSerializer.Serialize(playerData, new JsonSerializerOptions { WriteIndented = false });
            byte[] salt = Convert.FromBase64String(playerData.Salt);
            byte[] key = CryptoService.DeriveKey(password, salt);
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes(json);
            var (ciphertext, nonce, tag) = CryptoService.Encrypt(plaintext, key);
            
            var saveDoc = new EncryptedSaveDoc
            {
                Username = username,
                Ciphertext = Convert.ToBase64String(ciphertext),
                Nonce = Convert.ToBase64String(nonce),
                Tag = Convert.ToBase64String(tag),
                Salt = playerData.Salt,
                HighScore = playerData.HighScore,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Upsert (update if exists, insert if not)
            var filter = Builders<EncryptedSaveDoc>.Filter.Eq(x => x.Username, username);
            var options = new ReplaceOptions { IsUpsert = true };
            await _context.Saves.ReplaceOneAsync(filter, saveDoc, options);
            
            return saveDoc;
        }
        
        public async Task<PlayerData?> LoadPlayerDataAsync(string username, string password)
        {
            var saveDoc = await GetSaveByUsernameAsync(username);
            if (saveDoc == null) return null;
            
            try
            {
                byte[] ciphertext = Convert.FromBase64String(saveDoc.Ciphertext);
                byte[] nonce = Convert.FromBase64String(saveDoc.Nonce);
                byte[] tag = Convert.FromBase64String(saveDoc.Tag);
                byte[] salt = Convert.FromBase64String(saveDoc.Salt);
                
                byte[] key = CryptoService.DeriveKey(password, salt);
                byte[] plaintext = CryptoService.Decrypt(ciphertext, nonce, tag, key);
                string json = System.Text.Encoding.UTF8.GetString(plaintext);
                
                var playerData = JsonSerializer.Deserialize<PlayerData>(json);
                return playerData;
            }
            catch (Exception)
            {
                // Decryption failed - wrong password
                return null;
            }
        }
        
        public async Task<List<EncryptedSaveDoc>> GetLeaderboardAsync(int limit = 10)
        {
            return await _context.Saves
                .Find(FilterDefinition<EncryptedSaveDoc>.Empty)
                .SortByDescending(x => x.HighScore)
                .Limit(limit)
                .ToListAsync();
        }
        
        public async Task<bool> DeletePlayerDataAsync(string username)
        {
            var profileFilter = Builders<ProfileDoc>.Filter.Eq(x => x.Username, username);
            var saveFilter = Builders<EncryptedSaveDoc>.Filter.Eq(x => x.Username, username);
            
            var profileResult = await _context.Profiles.DeleteOneAsync(profileFilter);
            var saveResult = await _context.Saves.DeleteOneAsync(saveFilter);
            
            return profileResult.DeletedCount > 0 || saveResult.DeletedCount > 0;
        }
        
        public async Task<long> GetTotalPlayersAsync()
        {
            return await _context.Profiles.CountDocumentsAsync(FilterDefinition<ProfileDoc>.Empty);
        }
    }
}