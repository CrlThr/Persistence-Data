using System;

namespace Game
{
    class Program
    {
        static void Main(string[] args)
        {
            DungeonGenerator generator = new DungeonGenerator();
            Map map = generator.GenerateDungeon(200, 50);

            //find a empty space to place the player
            int Startx = 1, Starty = 1;
            while (map.Tiles[Starty][Startx] != Tile.Empty)
            {
                Startx++;
                if (Startx >= map.Width)
                {
                    Startx = 0;
                    Starty++;
                }
                if (Starty >= map.Height)
                    break;
            }
            Player player = new Player(Startx, Starty, '@');
            Console.CursorVisible = false;

            while (true)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                player.Move(map);
                player.HandleInput(map);
                System.Threading.Thread.Sleep(20);
            }
        }
    }
}


