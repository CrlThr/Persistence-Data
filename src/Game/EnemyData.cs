
namespace Game
{
    public class Enemy
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public int Damage { get; set; }
        public char Symbol { get; set; }
        public DateTime LastAttackTime { get; set; } = DateTime.MinValue;
        public double AttackCooldown { get; set; } = 3.0; // seconds between attacks
        public double HealDropChance { get; set; } = 0.0; // 0 percent default 
        public int HealAmount { get; set; } = 0; // Amount of health restored by potion

        public Enemy(int x, int y, int health, int damage, char symbol)
        {
            X = x;
            Y = y;
            Health = health;
            Damage = damage;
            Symbol = symbol;
        }
    }
}