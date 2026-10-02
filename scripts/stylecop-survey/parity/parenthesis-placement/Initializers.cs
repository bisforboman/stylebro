namespace P
{
    public class Point
    {
        public Point(int x, int y)
        {
        }

        public Point(int x)
            : this(
                x,
                0
            )
        {
        }
    }

    public class Point3 : Point
    {
        public Point3(int x, int y, int z)
            : base(
                x,
                y
            )
        {
        }

        public static Point Origin() => new(
            0,
            0
        );
    }
}
