
using System;
using System.Collections.Generic;
using Game;

namespace Game
{
    public class EnemyManager
    {
        private static readonly Random rng = new Random();

        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public List<HealPickup> HealPickups { get; private set; } = new List<HealPickup>();
        
        private static void SafeSetCursorPosition(int x, int y)
        {
            try
            {
                // Check bounds before setting cursor position
                if (x >= 0 && y >= 0 && x < Console.WindowWidth && y < Console.WindowHeight)
                {
                    Console.SetCursorPosition(x, y);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Silently ignore out of bounds cursor positions
            }
            catch (Exception)
            {
                // Silently ignore other console-related exceptions
            }
        }

        public void AddEnemy(Enemy enemy)
        {
            Enemies.Add(enemy);
        }

        public Enemy? GetEnemyAt(int x, int y)
        {
            foreach (var enemy in Enemies)
            {
                if (enemy.X == x && enemy.Y == y)
                    return enemy;
            }
            return null;
        }


        public void UpdateEnemies(Player player, Map map)
        {
            foreach (var enemy in Enemies.ToList())
            {
                int dx = player.X - enemy.X;
                int dy = player.Y - enemy.Y;

                int distance = Math.Abs(dx) + Math.Abs(dy); // abslute value 


                if (distance == 1)
                {
                    if ((DateTime.Now - enemy.LastAttackTime).TotalSeconds >= enemy.AttackCooldown)
                    {
                        player.Health -= enemy.Damage;
                        enemy.LastAttackTime = DateTime.Now;
                        player.TemporaryMessage($"An enemy attacked you! -{enemy.Damage} HP",ConsoleColor.White);
                    }

                    if (player.Health <= 0)
                    {
                        int statsX = Math.Min(Console.WindowWidth - 25, 200 + 2);
                        int statsY = Math.Min(Console.WindowHeight - 5, 23);
                        SafeSetCursorPosition(statsX, statsY);
                        Console.WriteLine("Game Over, You died!");
                        Thread.Sleep(2000);
                        Environment.Exit(0);
                    }

                    continue; // don't move if we already attack
                }
                int newX = enemy.X;
                int newY = enemy.Y;

                // 50% chance to move towards player or randomly
                Random rng = new Random();

                if (rng.Next(2) == 0)
                {
                    // approach player
                    if (Math.Abs(dx) > Math.Abs(dy))
                        newX += Math.Sign(dx); // move horizontally
                    else
                        newY += Math.Sign(dy); // move vertically
                }
                else
                {
                    // move randomly
                    int dir = rng.Next(4);
                    switch (dir)
                    {
                        case 0: newX++; break; // right
                        case 1: newX--; break; // left
                        case 2: newY++; break; // down
                        case 3: newY--; break; // up
                    }

                }

                // Check if nex case is walkable and free
                bool canMove =
                newX >= 0 && newX >= 0 &&
                newX < map.Width && newY < map.Height &&
                map.Tiles[newY][newX] == Tile.Empty &&
                !Enemies.Any(e => e != enemy && e.X == newX && e.Y == newY) &&
                !(player.X == newX && player.Y == newY); // not walk on player

                if (canMove)
                {
                    enemy.X = newX;
                    enemy.Y = newY;
                }

            }
        }
        
        public void DropHeal(Enemy enemy, Map map, Player player)
        {
            if (rng.Next(100) < 50)
            {
                // all directions: up, down, left, right
                int[,] directions = new int[,] { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
              
                //list of valid positions around the enemy
                List<(int x, int y)> validSpots = new List<(int, int)>();

                for (int i = 0; i < 4; i++)
                {
                    int dropX = enemy.X + directions[i, 0];
                    int dropY = enemy.Y + directions[i, 1];

                    // Check if the position is within bounds and empty
                    if (dropX >= 0 && dropX < map.Width &&
                        dropY >= 0 && dropY < map.Height &&
                        map.Tiles[dropY][dropX] == Tile.Empty &&
                        !HealPickups.Any(h => h.X == dropX && h.Y == dropY)) 
                    {
                        validSpots.Add((dropX, dropY));
                    }
                }

                // if no valid spots, drop nothing 
                if (validSpots.Count == 0)
                    return;
                  
                // choose a random valid spot
                var (finalDropX, finalDropY) = validSpots[rng.Next(validSpots.Count)];
                
                //create and add the heal pickup
                HealPickup heal = new HealPickup(finalDropX, finalDropY, enemy.HealAmount);
                HealPickups.Add(heal);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                SafeSetCursorPosition(finalDropX, finalDropY);
                Console.Write(heal.Symbol); // '♥'
                Console.ResetColor();

                // text output on the right side
                player.TemporaryMessage($"Heal collected at ({finalDropX},{finalDropY})!", ConsoleColor.White);
               
                   
            }
        }
    }
}
