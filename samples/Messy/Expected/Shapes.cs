namespace Messy;

public class Shapes
{
    private int _count;

    public sealed class Circle
    {
        public double Radius { get; init; }

        public double Area() => Math.PI * Radius * Radius;
    }
}
