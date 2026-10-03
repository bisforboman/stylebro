namespace Messy.Totals
{
    public class Totals
    {
        public int Sum(int a, int b)
        {
            var total = (a + b);
            var scaled = a + (b * 2);
            return (total + scaled);
        }
    }
}
