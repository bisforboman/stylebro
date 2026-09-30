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

        public string Raw(int x) => $"""

            {x}

            """;
    }
}