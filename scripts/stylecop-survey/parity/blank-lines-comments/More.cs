namespace Probe2
{
    public class More
    {
        [System.Obsolete]
        // after an attribute
        public void M(int x)
        {
            var a = 1;
            ////four slashes after code
            var b = 2; // trailing
            // after a line ending in a comment
            if (x > 0)
            {
            }
            // after a closing brace
            M(
                // first argument comment
                1);
            var arr = new[]
            {
                1,
                // between items
                2,
            };
            var s = x switch
            {
                1 => "a",
                // between arms
                _ => "b",
            };
        label:
            // after a label
            return;
        }

        public int[] Collection() =>
        [
            // first in a collection expression (StyleCop #3766, fixed after 1.2.0-beta.556)
            1,
        ];

        public string Raw(int x) => $"""

            {x}

            """;
    }
}