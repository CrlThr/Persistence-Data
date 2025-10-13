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

    class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char symbol;
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
            
            // Display controls
            Console.SetCursorPosition(statsX, statsY + 4);
            Console.Write("Controls:");
            
            Console.SetCursorPosition(statsX, statsY + 5);
            Console.Write("Arrows: Move");
            
            Console.SetCursorPosition(statsX, statsY + 6);
            Console.Write("R: Reset Score");
            
            Console.SetCursorPosition(statsX, statsY + 7);
            Console.Write("ESC: Quit");
            
            Console.SetCursorPosition(statsX, statsY + 10);
            Console.Write("Find the X to");
            
            Console.SetCursorPosition(statsX, statsY + 11);
            Console.Write("advance floors!");
            
            Console.SetCursorPosition(statsX, statsY + 13);
            Console.Write("Progress is saved");
            
            Console.SetCursorPosition(statsX, statsY + 14);
            Console.Write("automatically!");
        }

        public void Render(Map map)
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
                
                string output = "";
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (x == X && y == Y)
                        {
                            output += symbol;
                        }
                        else
                        {
                            output += GetVisibleTileChar(map, x, y);
                        }
                    }
                    output += "\n";
                }
                Console.Write(output);
                
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
                            Console.Write(GetVisibleTileChar(map, x, y));
                        }
                    }
                }
            }

            lastX = X;
            lastY = Y;
        }
        
        public void UpdateStats(Map map)
        {
            // Method to refresh stats display without full render
            DisplayStats(map);
        }
        
        public bool HandleInput(Map map)
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
    }
}