namespace Messy.Guards
{
    public class Guards
    {
        public int Clamp(int value, int max)
        {
            if (value < 0)
                return 0;
            else if (value > max) return max;

            for (var i = 0; i < 2; i++)
                if (i == value)
                    value++;

            return value;
        }
    }
}
