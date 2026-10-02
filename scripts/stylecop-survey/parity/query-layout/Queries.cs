namespace P
{
    using System.Linq;

    public class Queries
    {
        public object Blank(int[] items) =>
            from x in items

            where x > 0
            select x;

        public object Mixed(int[] items)
        {
            var q = from x in items where x > 0 orderby x
                select x;
            return q;
        }

        public object AfterMultiLine(int[] items) =>
            from x in items
            where x > 0 &&
                x < 10 select x;

        public object MultiLine(int[] items) =>
            from x in items where x > 0 &&
                x < 10
            select x;

        public object Continuation(int[] items) =>
            from x in items
            group x by x % 2 into g select g.Key;

        public object OneLine(int[] items) => from x in items where x > 0 select x;

        public object OwnLines(int[] items) =>
            from x in items
            where x > 0
            select x;

        public object Commented(int[] items) =>
            from x in items /* positive */ where x > 0
            select x;

        public object CommentLine(int[] items) =>
            from x in items

            // only positive ones

            where x > 0
            select x;

        public object InParentheses(int[] items) =>
            (from x in items where x > 0
            select x).ToList();

        public object MixedAndMultiLine(int[] items) =>
            from x in items where x > 0 orderby x.ToString()
                .Length
            select x;
    }
}
