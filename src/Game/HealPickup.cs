namespace Game
{
    public class HealPickup
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int HealAmount { get; set; }
        public char Symbol { get; set; } = '♥';

        public HealPickup(int x, int y, int healAmount)
        {
            X = x;
            Y = y;
            HealAmount = healAmount;
        }
    }
}