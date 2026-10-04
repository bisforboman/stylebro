namespace Probe
{
    using System;
    using System.Collections.Generic;

    public class MultiLineFields
    {
        private readonly List<int> multiLine = new List<int>
        {
            1,
        };
        private int afterMultiLine;
        private int single;
        private readonly int[] multiLineBelow =
        {
            2,
        };

        [Obsolete]
        private int attributed;
        private int afterAttributed;

        [Obsolete]
        private int[] attributedMultiLine = new int[]
        {
            3,
        };
        private int afterAttributedMultiLine;

        private int first, second;
        private string longInitializer =
            "on the next line";
        private int afterLongInitializer;

        // A comment.
        private int commented;
        private int last;
    }
}
