using System;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            DungeonGenerator generator = new DungeonGenerator();
            Map map = generator.GenerateDungeon(200, 50);

            // Display the generated map
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Console.Write(map.Tiles[y][x] == Tile.Empty ? '.' : '#');
                }
                Console.Write("\n");
            }
        }
    }
}
