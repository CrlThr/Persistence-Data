using System.Security.Cryptography.X509Certificates;

namespace Game
{
    public struct PlayerStats
    {
        public string Name { get; set; }
        public int CurrentScore { get; set; }
        public int HighScore { get; set; }

        public PlayerStats(string name, int highScore = 0)
        {
            Name = name;
            CurrentScore = 0;
            HighScore = highScore;
        }

        public void IncrementScore()
        {
            CurrentScore++;
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
            }
        }

        public void ResetCurrentScore()
        {
            CurrentScore = 0;
        }
    }

    public class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char symbol;
        public int Health { get; set; } = 100;
        public PlayerStats Stats { get; set; }
        private int lastX = -1;
        private int lastY = -1;
        private bool firstRender = true;
        private const int VISION_RADIUS = 4; // How far the player can see
        private HashSet<Room> enteredRooms = new HashSet<Room>(); // Track which rooms have been entered

        public Player(int x, int y, char symbol, string playerName, int highScore = 0)
        {
            X = x;
            Y = y;
            this.symbol = symbol;
            Stats = new PlayerStats(playerName, highScore);
        }

        public void ResetPosition(int x, int y)
        {
            X = x;
            Y = y;
            lastX = -1;
            lastY = -1;
            firstRender = true;
            enteredRooms.Clear(); // Clear room history for new map
        }

        private void UpdateVisibility(Map map)
        {
            // Check if player is in a room
            Room? currentRoom = map.GetRoomAt(X, Y);
            if (currentRoom != null && !enteredRooms.Contains(currentRoom))
            {
                // Player entered a new room - reveal the entire room
                currentRoom.RevealRoom(map.Explored);
                enteredRooms.Add(currentRoom);
            }

            // Update explored status for tiles within vision radius (for corridors and areas outside rooms)
            for (int dy = -VISION_RADIUS; dy <= VISION_RADIUS; dy++)
            {
                for (int dx = -VISION_RADIUS; dx <= VISION_RADIUS; dx++)
                {
                    int checkX = X + dx;
                    int checkY = Y + dy;

                    // Check if within map bounds
                    if (checkX >= 0 && checkX < map.Width && checkY >= 0 && checkY < map.Height)
                    {
                        // Calculate distance using Manhattan distance for simplicity
                        int distance = Math.Abs(dx) + Math.Abs(dy);
                        if (distance <= VISION_RADIUS)
                        {
                            map.Explored[checkY][checkX] = true;
                        }
                    }
                }
            }
        }

        private bool IsVisible(Map map, int x, int y)
        {
            // A tile is visible if it's within vision radius of the player
            int distance = Math.Abs(x - X) + Math.Abs(y - Y);
            return distance <= VISION_RADIUS;
        }

        private char GetVisibleTileChar(Map map, int x, int y)
        {
            if (IsVisible(map, x, y))
            {
                // Player can currently see this tile
                return map.Tiles[y][x] switch
                {
                    Tile.Empty => '.',
                    Tile.Exit => 'X',
                    _ => '█'
                };
            }
            else if (map.Explored[y][x])
            {
                // Player has seen this tile before but can't see it now
                return map.Tiles[y][x] switch
                {
                    Tile.Empty => '.', // Dimmed empty space
                    Tile.Exit => 'X',  // Exit remains visible once discovered
                    _ => '█' // Dimmed wall
                };
            }
            else
            {
                // Completely unexplored - fog of war
                return ' ';
            }
        }

        private void DisplayStats(Map map)
        {
            // Display player stats on the right side of the screen
            int statsX = map.Width + 2;
            int statsY = 2;

            Console.SetCursorPosition(statsX, statsY);
            Console.Write($"Player: {Stats.Name}");

            Console.SetCursorPosition(statsX, statsY + 1);
            Console.Write($"Floor: {Stats.CurrentScore}");

            Console.SetCursorPosition(statsX, statsY + 2);
            Console.Write($"Best: {Stats.HighScore}");

            Console.SetCursorPosition(statsX, statsY + 3);
            string healthBar = new string('█', Health / 10).PadRight(10, '.');
            Console.Write($"Health: [{healthBar}] {Health} HP");
            // Display controls
            Console.SetCursorPosition(statsX, statsY + 4);
            Console.Write("Controls:");

            Console.SetCursorPosition(statsX, statsY + 5);
            Console.Write("Arrows: Move");

            Console.SetCursorPosition(statsX, statsY + 6);
            Console.Write("R: Reset Score");

            Console.SetCursorPosition(statsX, statsY + 7);
            Console.Write("ESC: Quit");

            Console.SetCursorPosition(statsX, statsY + 8);
            Console.Write("A: Attack");

            Console.SetCursorPosition(statsX, statsY + 10);
            Console.Write("Find the X to");

            Console.SetCursorPosition(statsX, statsY + 11);
            Console.Write("advance floors!");

            Console.SetCursorPosition(statsX, statsY + 13);
            Console.Write("Progress is saved");

            Console.SetCursorPosition(statsX, statsY + 14);
            Console.Write("automatically!");
        }

        public void Render(Map map, List<Enemy> enemies, EnemyManager enemyManager)
        {
            // Update visibility based on current position
            UpdateVisibility(map);

            // Only render if this is the first time or if the player has moved
            if (!firstRender && lastX == X && lastY == Y)
                return;

            if (firstRender)
            {
                // First render: draw the entire map with fog of war
                Console.Clear();
                Console.SetCursorPosition(0, 0);

               for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        Console.SetCursorPosition(x, y);

                        if (x == X && y == Y)
                        {
                            Console.Write(symbol);
                        }
                        else
                        {
                            var enemyHere = enemies.Find(e => e.X == x && e.Y == y);
                            var healHere = enemyManager.HealPickups.FirstOrDefault(h => h.X == x && h.Y == y);
                            if (enemyHere != null && IsVisible(map, x, y))
                            {
                                Console.Write(enemyHere.Symbol);
                                Console.ResetColor();
                            }
                           
                            else if(healHere != null && IsVisible(map, x, y))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write(healHere.Symbol);
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.Write(GetVisibleTileChar(map, x, y));
                            }
                        }
                    }
                }
                
                // Display stats on first render
                DisplayStats(map);
                firstRender = false;
            }
            else
            {
                // Player moved: need to update visibility area around both old and new positions
                // Update a larger area to handle visibility changes
                int minX = Math.Max(0, Math.Min(lastX, X) - VISION_RADIUS - 1);
                int maxX = Math.Min(map.Width - 1, Math.Max(lastX, X) + VISION_RADIUS + 1);
                int minY = Math.Max(0, Math.Min(lastY, Y) - VISION_RADIUS - 1);
                int maxY = Math.Min(map.Height - 1, Math.Max(lastY, Y) + VISION_RADIUS + 1);

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Console.SetCursorPosition(x, y);
                        if (x == X && y == Y)
                        {
                            Console.Write(symbol);
                        }
                        else
                        {
                        var enemyHere = enemies.Find(e => e.X == x && e.Y == y);
                        if (enemyHere != null && IsVisible(map, x, y))
                            Console.Write(enemyHere.Symbol);
                        else
                            Console.Write(GetVisibleTileChar(map, x, y));
                        }
                    }
                }
            }

            lastX = X;
            lastY = Y;

            // Always update stats after rendering
            DisplayStats(map);
        }

        public void UpdateStats(Map map)
        {
            // Method to refresh stats display without full render
            DisplayStats(map);
        }

        public bool HandleInput(Map map, List<Enemy> enemies, EnemyManager enemyManager)
        {
            if (!Console.KeyAvailable)
                return false;

            ConsoleKey key = Console.ReadKey(true).Key;

            int newX = X;
            int newY = Y;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    newY--;
                    break;
                case ConsoleKey.DownArrow:
                    newY++;
                    break;
                case ConsoleKey.LeftArrow:
                    newX--;
                    break;
                case ConsoleKey.RightArrow:
                    newX++;
                    break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
                case ConsoleKey.R:
                    // Reset current score (but keep high score)
                    var stats = Stats;
                    stats.ResetCurrentScore();
                    Stats = stats;
                    return false;
                case ConsoleKey.A:
                    Attack(enemies, enemyManager);
                    break;
            }

          

            // Check for collisions with walls
            if (newX >= 0 && newY >= 0 && newX < map.Width && newY < map.Height)
            {
                if (map.Tiles[newY][newX] == Tile.Empty || map.Tiles[newY][newX] == Tile.Exit)
                {
                    X = newX;
                    Y = newY;

                    // Check if player reached the exit
                    if (map.Tiles[newY][newX] == Tile.Exit)
                    {
                        return true; // Exit reached
                    }
                }
            }

            return false; // Exit not reached
        }
        public void Attack(List<Enemy> enemies, EnemyManager enemyManager)
        {
            Enemy? target = null;
            Random rng = new Random();

            //check the 4 directions of the player 
            int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };

            for (int i = 0; i < 4; i++)
            {
                int checkX = X + directions[i, 0];
                int checkY = Y + directions[i, 1];

                foreach (var enemy in enemies)
                {

                    if (enemy.X == checkX && enemy.Y == checkY)
                    {
                        enemy.Health -= 15;
                        

                        if (enemy.Health <= 0)
                        {
                            target = enemy;
                        }

                        break;
                    }
                }
            }

            if (target != null)
            {
                enemies.Remove(target);
                Console.WriteLine("Enemy defeated!");
                enemyManager.DropHeal(target);
            }
            else
            {
                Console.SetCursorPosition(0, 9);
                Console.WriteLine("No enemy adjacent to attack.");
            }
        }
        
        public void CheckForHeal(EnemyManager enemyManager)
        {
            var heal = enemyManager.HealPickups.FirstOrDefault(h => h.X == this.X && h.Y == this.Y);
            if (heal != null)
            {
                Health = Math.Min(100, Health + heal.HealAmount);
                enemyManager.HealPickups.Remove(heal);
                Console.SetCursorPosition(0, 8);
                Console.WriteLine($" You picked up a heal! +{heal.HealAmount} HP (Now {Health} HP)");
            }
        }
    }
}
