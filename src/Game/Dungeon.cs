namespace Game
{
    public enum Tile
    {
        Empty,
        Solid,
        Exit
    }

    public class Room
    {
        public int X1;
        public int Y1;
        public int X2;
        public int Y2;

        public Room(int x1, int y1, int width, int height)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x1 + width;
            Y2 = y1 + height;
        }

        public (float, float) Center()
        {
            return ((X1 + X2) / 2f, (Y1 + Y2) / 2f);
        }

        public bool Intersects(Room other)
        {
            return (X1 <= other.X2 && X2 >= other.X1 &&
                    Y1 <= other.Y2 && Y2 >= other.Y1);
        }

        public bool Contains(int x, int y)
        {
            return x > X1 && x < X2 && y > Y1 && y < Y2;
        }

        public void RevealRoom(List<List<bool>> explored)
        {
            for (int y = Y1 + 1; y < Y2; y++)
            {
                for (int x = X1 + 1; x < X2; x++)
                {
                    if (y >= 0 && y < explored.Count && x >= 0 && x < explored[y].Count)
                    {
                        explored[y][x] = true;
                    }
                }
            }
        }
    }

    public class Map
    {
        public bool Generated = false;
        public int Width;
        public int Height;
        public required List<List<Tile>> Tiles;
        public required List<Room> Rooms;
        public required List<List<bool>> Explored; // Tracks which tiles have been explored

        public Room? GetRoomAt(int x, int y)
        {
            foreach (Room room in Rooms)
            {
                if (room.Contains(x, y))
                {
                    return room;
                }
            }
            return null;
        }
    }
}
