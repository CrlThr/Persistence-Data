using System;
using DataPersistence;

namespace Game
{
    class Program
    {
        private static PlayerData? currentPlayerData;
        private static string? currentPassword;
        
        static void Main(string[] args)
        {
            // Ensure Saves directory exists
            if (!System.IO.Directory.Exists("Saves"))
                System.IO.Directory.CreateDirectory("Saves");
            
            while (true)
            {
                // Authenticate user
                currentPlayerData = AuthManager.AuthenticateUser();
                
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
                StartGame();
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
                Console.WriteLine("2. Exit Game");
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
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
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
        
        static void StartGame()
        {
            if (currentPlayerData == null || string.IsNullOrEmpty(currentPassword))
                return;
            
            DungeonGenerator generator = new DungeonGenerator();
            Map map = generator.GenerateDungeon(200, 50);
            Player player = new Player(0, 0, '@', currentPlayerData.Name, currentPlayerData.HighScore);
            
            // Find initial starting position and set player
            var (startX, startY) = FindStartingPosition(map);
            player.ResetPosition(startX, startY);
            
            Console.CursorVisible = false;
            
            bool gameRunning = true;
            while (gameRunning)
            {
                player.Render(map);
                bool exitReached = player.HandleInput(map);
                
                if (exitReached)
                {
                    // Increment score when exit is reached
                    var stats = player.Stats;
                    stats.IncrementScore();
                    player.Stats = stats;
                    
                    // Save progress
                    AuthManager.SavePlayerProgress(currentPlayerData, currentPassword, stats.HighScore);
                    
                    // Generate new map and reset player position
                    map = generator.GenerateDungeon(200, 50);
                    var (newStartX, newStartY) = FindStartingPosition(map);
                    player.ResetPosition(newStartX, newStartY);
                    
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
                        AuthManager.SavePlayerProgress(currentPlayerData, currentPassword, player.Stats.HighScore);
                        gameRunning = false;
                    }
                }
                
                System.Threading.Thread.Sleep(20); // ~60 FPS
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
    }
}


