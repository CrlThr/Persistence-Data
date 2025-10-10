namespace Game
{
    class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char symbol;

        public Player(int x, int y, char symbol)
        {
            X = x;
            Y = y;
            this.symbol = symbol;
        }
        
        public void Move(Map map)
        {
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
                        output += map.Tiles[y][x] == Tile.Empty ? '.' : '#';
                    }
                }
                output += "\n";
            }
            Console.Write(output);
        }
        
        public void HandleInput(Map map)
        {
            if (!Console.KeyAvailable)
                return;

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
            }
            
            // Check for collisions with walls
            if (newX >= 0 && newY >= 0 && newX < map.Width && newY < map.Height)
            {
                if (map.Tiles[newY][newX] == Tile.Empty)
                {
                    X = newX;
                    Y = newY;
                }
            }
        }
    }
}