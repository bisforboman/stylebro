namespace Messy;

public class Shapes
{
    public sealed class Circle
    {
        public double Area() => Math.PI * Radius * Radius;

        public double Radius { get; init; }
    }

    private int _count;
}
