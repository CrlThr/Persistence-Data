using System;

namespace Game
{
    [Serializable]
    public class PlayerData
    {
        public string Name { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Salt { get; set; } = "";
        public int HighScore { get; set; } = 0;
        public DateTime LastPlayed { get; set; } = DateTime.Now;
        public int TotalGamesPlayed { get; set; } = 0;
        
        public PlayerData() { }
        
        public PlayerData(string name, string passwordHash, string salt)
        {
            Name = name;
            PasswordHash = passwordHash;
            Salt = salt;
            HighScore = 0;
            LastPlayed = DateTime.Now;
            TotalGamesPlayed = 0;
        }
    }
}