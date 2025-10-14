
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
                        Console.SetCursorPosition(0, 1);
                        Console.WriteLine($"Enemy Attack you! -{enemy.Damage} HP");
                    }

                    if (player.Health <= 0)
                    {
                        Console.SetCursorPosition(0, 2);
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
        public void DropHeal(Enemy enemy)
        {
            if (rng.Next(100) < 50)
            {
                HealPickups.Add(new HealPickup(enemy.X, enemy.Y, enemy.HealAmount));
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"A heal dropped at ({enemy.X},{enemy.Y})!");
                   
            }
        }
    }
}
