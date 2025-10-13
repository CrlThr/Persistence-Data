
using System;
using System.Collections.Generic;
using Game;

namespace Game
{
    public class EnemyManager
    {
    private static readonly Random rng = new Random();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

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

        // Déplace chaque ennemi aléatoirement ou vers le joueur, et attaque si sur la même case
        public void UpdateEnemies(Player player, Map map)
        {
            foreach (var enemy in Enemies)
            {
                int dx = player.X - enemy.X;
                int dy = player.Y - enemy.Y;
                int distance = Math.Abs(dx) + Math.Abs(dy);

                // Si le joueur est proche (rayon 3), poursuite
                if (distance <= 3)
                {
                    if (Math.Abs(dx) > Math.Abs(dy))
                        enemy.X += Math.Sign(dx);
                    else if (dy != 0)
                        enemy.Y += Math.Sign(dy);
                }
                else
                {
                    // Déplacement aléatoire
                    int dir = rng.Next(4);
                    int newX = enemy.X;
                    int newY = enemy.Y;
                    switch (dir)
                    {
                        case 0: newY--; break; // haut
                        case 1: newY++; break; // bas
                        case 2: newX--; break; // gauche
                        case 3: newX++; break; // droite
                    }
                    // Vérifie que la case est vide et dans la map
                    if (newX >= 0 && newY >= 0 && newX < map.Width && newY < map.Height && map.Tiles[newY][newX] == Tile.Empty)
                    {
                        enemy.X = newX;
                        enemy.Y = newY;
                    }
                }

                // Si l'ennemi est sur la même case que le joueur, attaque
                if (enemy.X == player.X && enemy.Y == player.Y)
                {
                    player.Health -= enemy.Damage;
                    Console.WriteLine($"Le joueur est attaqué par un ennemi ! -{enemy.Damage} HP");

                    if (player.Health <= 0)
                    {
                        Console.WriteLine("Game Over, You dead!");
                        Environment.Exit(0);
                    }
                }
            }
        }
    }
}
