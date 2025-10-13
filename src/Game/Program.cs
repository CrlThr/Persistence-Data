using System;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get player name
            Console.Clear();
            Console.Write("Enter your name: ");
            string? playerName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Anonymous";

            DungeonGenerator generator = new DungeonGenerator();
            Map map = generator.GenerateDungeon(200, 50);
            Player player = new Player(0, 0, '@', playerName);
            
            // Find initial starting position and set player
            var (startX, startY) = FindStartingPosition(map);
            player.ResetPosition(startX, startY);
            
            Console.CursorVisible = false;

            while (true)
            {
                player.Render(map);
                bool exitReached = player.HandleInput(map);
                
                if (exitReached)
                {
                    // Increment score when exit is reached
                    var stats = player.Stats;
                    stats.IncrementScore();
                    player.Stats = stats;
                    
                    // Generate new map and reset player position
                    map = generator.GenerateDungeon(200, 50);
                    var (newStartX, newStartY) = FindStartingPosition(map);
                    player.ResetPosition(newStartX, newStartY);
                    
                    // Update stats display
                    player.UpdateStats(map);
                }
                
                // Check if stats were updated (for reset functionality)
                player.UpdateStats(map);
                
                System.Threading.Thread.Sleep(20); // ~60 FPS
            }
        }
        
        static (int, int) FindStartingPosition(Map map)
        {
            // Find an empty space to place the player
            int startX = 1, startY = 1;
            while (map.Tiles[startY][startX] != Tile.Empty)
            {
                startX++;
                if (startX >= map.Width)
                {
                    startX = 0;
                    startY++;
                }
                if (startY >= map.Height)
                    break;
            }
            return (startX, startY);
        }
    }
}


