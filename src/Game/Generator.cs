namespace Game
{
    class DungeonGenerator
    {
        public int MaxRooms = 20;
        public int RoomMinSize = 10;
        public int RoomMaxSize = 20;

        public Map GenerateDungeon(int width, int height)
        {
            Map map = new Map
            {
                Width = width,
                Height = height,
                Tiles = new List<List<Tile>>(),
                Rooms = new List<Room>(),
                Explored = new List<List<bool>>()
            };

            // Initialize map with solid tiles and unexplored areas
            for (int y = 0; y < height; y++)
            {
                List<Tile> row = new List<Tile>();
                List<bool> exploredRow = new List<bool>();
                for (int x = 0; x < width; x++)
                {
                    row.Add(Tile.Solid);
                    exploredRow.Add(false); // All tiles start unexplored
                }
                map.Tiles.Add(row);
                map.Explored.Add(exploredRow);
            }

            // Dungeon generation logic goes here
            Random rng = new Random();
            int numRooms = 0;
            for (int r = 0; r < MaxRooms; r++)
            {
                int w = rng.Next(RoomMinSize, RoomMaxSize + 1);
                int h = rng.Next(RoomMinSize, RoomMaxSize + 1);
                int x = rng.Next(1, width - w - 1);
                int y = rng.Next(1, height - h - 1);

                Room newRoom = new Room(x, y, w, h);
                bool failed = false;
                foreach (Room otherRoom in map.Rooms)
                {
                    if (newRoom.Intersects(otherRoom))
                    {
                        failed = true;
                        break;
                    }
                }

                if (!failed)
                {
                    CreateRoom(map, newRoom);
                    if (numRooms > 0)
                    {
                        var (newX, newY) = newRoom.Center();
                        var (prevX, prevY) = map.Rooms[numRooms - 1].Center();

                        if (rng.Next(0, 2) == 1)
                        {
                            CreateHTunnel(map, (int)prevX, (int)newX, (int)prevY);
                            CreateVTunnel(map, (int)prevY, (int)newY, (int)newX);
                        }
                        else
                        {
                            CreateVTunnel(map, (int)prevY, (int)newY, (int)prevX);
                            CreateHTunnel(map, (int)prevX, (int)newX, (int)newY);
                        }
                    }

                    map.Rooms.Add(newRoom);
                    numRooms++;
                }
            }

            // Place a random exit in one of the rooms
            if (map.Rooms.Count > 0)
            {
                Room exitRoom = map.Rooms[rng.Next(map.Rooms.Count)];
                var (centerX, centerY) = exitRoom.Center();
                map.Tiles[(int)centerY][(int)centerX] = Tile.Exit;
            }

            map.Generated = true;
            return map;
        }

        private void CreateRoom(Map map, Room room)
        {
            for (int x = room.X1 + 1; x < room.X2; x++)
            {
                for (int y = room.Y1 + 1; y < room.Y2; y++)
                {
                    map.Tiles[y][x] = Tile.Empty;
                }
            }
        }

        private void CreateHTunnel(Map map, int x1, int x2, int y)
        {
            for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            {
                map.Tiles[y][x] = Tile.Empty;
            }
        }
        
        private void CreateVTunnel(Map map, int y1, int y2, int x)
        {
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                map.Tiles[y][x] = Tile.Empty;
            }
        }
    }
}