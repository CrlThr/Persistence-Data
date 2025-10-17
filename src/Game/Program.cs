using System;
using MongoDB.Driver;
using MongoDB.Bson;
using DataPersistence;
using Game.Services;
using Game.Models;

namespace Game
{
    class Program
    {
        private static PlayerData? currentPlayerData;
        private static string? currentPassword;
        private static MongoContext? mongoContext;
        private static MongoDbService? mongoService;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Try to initialize MongoDB connection, fallback to file system if it fails
            bool useMongoDb = false;
            try
            {
                Console.WriteLine("Attempting to connect to MongoDB...");
                mongoContext = new MongoContext("mongodb://localhost:27017", "game");
                mongoService = new MongoDbService(mongoContext);
                
                // Test connection with timeout
                await mongoContext.Db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                
                AuthManager.Initialize(mongoService);
                Console.WriteLine("✓ Connected to MongoDB successfully!");
                
                // Display stats
                var totalPlayers = await mongoService.GetTotalPlayersAsync();
                Console.WriteLine($"✓ Total registered players in database: {totalPlayers}");
                useMongoDb = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ MongoDB connection failed: {ex.Message}");
                Console.WriteLine("✓ Falling back to local file storage...");
                
                // Initialize AuthManager without MongoDB service (uses file system)
                AuthManager.Initialize(null);
                useMongoDb = false;
            }
            
            Console.WriteLine($"✓ Using {(useMongoDb ? "MongoDB" : "Local Files")} for data storage");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            // Ensure Saves directory exists (for backward compatibility)
            if (!System.IO.Directory.Exists("Saves"))
                System.IO.Directory.CreateDirectory("Saves");

            while (true)
            {
                try
                {
                    // Authenticate user
                    currentPlayerData = await AuthManager.AuthenticateUserAsync();
                    
                    if (currentPlayerData == null)
                    {
                        // User chose to return to main menu or authentication failed
                        ShowMainMenu();
                        continue;
                    }
                    
                    // Get password for saving (we need to store it for encryption)
                    currentPassword = GetPasswordForSession();
                    
                    if (string.IsNullOrEmpty(currentPassword))
                    {
                        Console.WriteLine("Session cancelled. Returning to main menu...");
                        Console.ReadKey();
                        continue;
                    }
                    
                    // Start game
                    await StartGameAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }
        
        static void ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== DUNGEON EXPLORER - MAIN MENU ===");
                Console.WriteLine();
                Console.WriteLine("1. Login / Create Account");
                Console.WriteLine("2. View Leaderboard");
                Console.WriteLine("3. Exit Game");
                Console.WriteLine();
                Console.Write("Select an option: ");
                
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();
                
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        return; // Return to authentication
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        ShowLeaderboardAsync().GetAwaiter().GetResult();
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        
        static async Task ShowLeaderboardAsync()
        {
            Console.Clear();
            Console.WriteLine("=== LEADERBOARD - TOP 10 PLAYERS ===");
            Console.WriteLine();
            
            if (mongoService != null)
            {
                // MongoDB leaderboard
                try
                {
                    var leaderboard = await mongoService.GetLeaderboardAsync(10);
                    
                    if (leaderboard.Count == 0)
                    {
                        Console.WriteLine("No players found in database.");
                    }
                    else
                    {
                        Console.WriteLine($"{"Rank",-5} {"Player",-20} {"High Score",-12} {"Last Played",-15}");
                        Console.WriteLine(new string('-', 60));
                        
                        for (int i = 0; i < leaderboard.Count; i++)
                        {
                            var save = leaderboard[i];
                            Console.WriteLine($"{i + 1,-5} {save.Username,-20} {save.HighScore,-12} {save.UpdatedAt:yyyy-MM-dd,-15}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading leaderboard: {ex.Message}");
                }
            }
            else
            {
                // File system leaderboard
                try
                {
                    Console.WriteLine("Reading local save files...");
                    var saveFiles = Directory.GetFiles("Saves", "*.json");
                    var playerScores = new List<(string Name, int Score, DateTime LastPlayed)>();
                    
                    foreach (var file in saveFiles)
                    {
                        try
                        {
                            string filename = Path.GetFileNameWithoutExtension(file);
                            string content = File.ReadAllText(file);
                            
                            // Try to extract high score from encrypted file metadata (if available)
                            // For file system, we can't easily decrypt without password, so show limited info
                            var lastWrite = File.GetLastWriteTime(file);
                            playerScores.Add((filename, 0, lastWrite)); // Score shows as 0 since we can't decrypt
                        }
                        catch
                        {
                            // Skip corrupted files
                        }
                    }
                    
                    if (playerScores.Count == 0)
                    {
                        Console.WriteLine("No local save files found.");
                    }
                    else
                    {
                        Console.WriteLine("Note: Scores not shown for local files (encrypted data)");
                        Console.WriteLine($"{"Rank",-5} {"Player",-20} {"Score",-12} {"Last Played",-15}");
                        Console.WriteLine(new string('-', 60));
                        
                        var sortedPlayers = playerScores.OrderByDescending(p => p.LastPlayed).Take(10);
                        int rank = 1;
                        foreach (var player in sortedPlayers)
                        {
                            Console.WriteLine($"{rank,-5} {player.Name,-20} {"Encrypted",-12} {player.LastPlayed:yyyy-MM-dd,-15}");
                            rank++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading local save files: {ex.Message}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to return to main menu...");
            Console.ReadKey();
        }
        
        static string? GetPasswordForSession()
        {
            Console.Clear();
            Console.WriteLine($"Welcome back, {currentPlayerData?.Name}!");
            Console.WriteLine("Please re-enter your password to start the game session:");
            
            return GetPasswordInput("Password: ");
        }
        
        static string GetPasswordInput(string prompt)
        {
            Console.Write(prompt);
            string password = "";
            ConsoleKeyInfo key;
            
            do
            {
                key = Console.ReadKey(true);
                
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            
            Console.WriteLine();
            return password;
        }
        
        static async Task StartGameAsync()
        {
            if (currentPlayerData == null || string.IsNullOrEmpty(currentPassword))
                return;
                
            EnemyManager enemyManager = new EnemyManager();
            DungeonGenerator generator = new DungeonGenerator();
            
            // Ensure the console is large enough, and generate a map that fits
            try 
            {
                // Only try to resize on Windows
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Console.SetWindowSize(Math.Min(120, Console.LargestWindowWidth), Math.Min(40, Console.LargestWindowHeight));
                }
            }
            catch
            {
                // If we can't resize, use current size
            }
            
            // Generate map that leaves space for UI (reduce width by 40 for stats, height by 5 for margins)
            int mapWidth = Math.Max(40, Console.WindowWidth - 40);
            int mapHeight = Math.Max(20, Console.WindowHeight - 5);
            
            Map map = generator.GenerateDungeon(mapWidth, mapHeight);
            Player player = new Player(0, 0, '@', currentPlayerData.Name, currentPlayerData.HighScore);
            
            SpawnEnemies(map, enemyManager, 3);
            player.ResetPosition(1, 1);
            player.CheckForHeal(enemyManager);
            player.TemporaryMessage("", ConsoleColor.White);
            
            // Find initial starting position and set player
            var (startX, startY) = FindStartingPosition(map);
            player.ResetPosition(startX, startY);
            
            Console.CursorVisible = false;
            
            bool gameRunning = true;
            while (gameRunning)
            {
                try
                {
                    player.Render(map, enemyManager.Enemies, enemyManager);
                    bool exitReached = player.HandleInput(map, enemyManager.Enemies, enemyManager);
                    enemyManager.UpdateEnemies(player, map);
              
                if (exitReached)
                {
                    // Increment score when exit is reached
                    var stats = player.Stats;
                    stats.IncrementScore();
                    player.Stats = stats;
                    
                    // Save progress to MongoDB
                    await AuthManager.SavePlayerProgressAsync(currentPlayerData, currentPassword, stats.HighScore);
                    
                    // Generate new map and reset player position
                    // Use same dynamic sizing as initial map generation
                    int newMapWidth = Math.Max(40, Console.WindowWidth - 40);
                    int newMapHeight = Math.Max(20, Console.WindowHeight - 5);
                    map = generator.GenerateDungeon(newMapWidth, newMapHeight);
                    var (newStartX, newStartY) = FindStartingPosition(map);
                    player.ResetPosition(newStartX, newStartY);
                    SpawnEnemies(map, enemyManager, 5);
                    
                    // Update stats display
                    player.UpdateStats(map);
                }
                
                // Check if player wants to quit (handle in HandleInput)
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        // Save final progress before quitting
                        await AuthManager.SavePlayerProgressAsync(currentPlayerData, currentPassword, player.Stats.HighScore);
                        gameRunning = false;
                    }
                }
                
                    System.Threading.Thread.Sleep(20); // ~60 FPS
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Handle console out of bounds exceptions gracefully
                    // This can happen if the console window is resized during gameplay
                    Console.Clear();
                    player.Render(map, enemyManager.Enemies, enemyManager);
                }
                catch (Exception ex) when (ex.Message.Contains("console") || ex.Message.Contains("cursor"))
                {
                    // Handle other console-related exceptions
                    System.Threading.Thread.Sleep(50); // Brief pause before continuing
                }
            }
            
            Console.CursorVisible = true;
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
        
        static void SpawnEnemies(Map map, EnemyManager enemyManager, int count)
        {
            Random rng = new Random();
            enemyManager.Enemies.Clear(); 

            for (int i = 0; i < count; i++)
            {
                int x, y;
                do
                {
                    x = rng.Next(map.Width);
                    y = rng.Next(map.Height);
                } while (map.Tiles[y][x] != Tile.Empty || enemyManager.GetEnemyAt(x, y) != null);

                var enemy = new Enemy(x, y, health: 30, damage: 8, symbol: 'E')
                {
                    HealDropChance = 0.5, // 50% chance to drop a health potion
                    HealAmount = 15     // Restores 15 HP
                };
                enemyManager.AddEnemy(enemy);
            }
        }
    }
}


