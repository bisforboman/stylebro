namespace Probe
{
    using System.Collections.Generic;

    public enum Multi
    {
        A,
        B
    }

    public enum Single { A, B }

    public class Point { public int X; public int Y; public Point Next; }

    public class Commas
    {
        public object M(int x, int[] items)
        {
            var array = new[]
            {
                1,
                2
            };
            var point = new Point
            {
                X = 1,
                Y = 2
            };
            var list = new List<int>
            {
                1,
                2
            };
            var anon = new
            {
                A = 1,
                B = 2
            };
            var dict = new Dictionary<int, int>
            {
                [1] = 1,
                [2] = 2
            };
            var sw = x switch
            {
                1 => "a",
                _ => "b"
            };
            int[] collection =
            [
                1,
                2
            ];
            var done = new[]
            {
                1,
                2,
            };
            var oneLine = new[] { 1, 2 };
            var comment = new[]
            {
                1,
                2 // last
            };
            var sameLineBrace = new[]
            {
                1,
                2 };
            bool pattern = point is
            {
                X: 1,
                Y: 2
            };
            bool nestedPattern = point is
            {
                X: 1,
                Next:
                {
                    Next: { Y: 2 }
                }
            };
            bool listPattern = items is
            [
                1,
                2
            ];
            return (array, point, list, anon, dict, sw, collection, done, oneLine, comment, sameLineBrace, pattern, nestedPattern, listPattern);
        }
    }
}
