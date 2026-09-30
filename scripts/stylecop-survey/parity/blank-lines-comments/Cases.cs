namespace Probe
{

    public class Cases
    {

        private int field;
        // after a field, no blank line
        private int other;

        public void M(int x)
        {
            // first line in a block
            var a = 1;
            // after a statement
            var b = 2;

            // after a blank line
            // second comment line
            var c = 3;
            switch (x)
            {
                case 1:
                    // after a case label
                    break;
                default:
                    break;
            }

            if (x > 0) { /* block */ }
            var d = new[]
            {

                1,
            };
            M(a + b + c); // trailing comment
            ////commented out
#if DEBUG
            // after a directive
#endif
        }
    }
}
