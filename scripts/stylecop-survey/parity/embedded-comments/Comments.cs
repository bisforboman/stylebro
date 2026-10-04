namespace Parity
{
    using System;

    public class Comments
    {
        public int Reported(int x, object o, int[] items)
        {
            if (x == 0) // trailing after ')'
            {
                return 0;
            }
            else if (x == 1) // else if
            {
                return 1;
            }
            else // else
            {
                x++;
            }

            if (x == 2)
            // own line between ')' and '{'
            {
                x++;
            }

            while (x > 10) /* block comment, K&R */ {
                x--;
            }

            foreach (var i in items) // foreach
            {
                x += i;
            }

            for (var i = 0; i < 2; i++) // for
            {
                x += i;
            }

            do // do
            {
                x--;
            }
            while (x > 5);

            lock (o) // lock
            {
                x++;
            }

            try // try
            {
                x = checked(x * 2);
            }
            catch (OverflowException) when (x > 0) // catch when
            {
                x = 0;
            }
            catch // bare catch
            {
                x = 1;
            }
            finally // finally
            {
                x++;
            }

            checked // checked
            {
                x++;
            }

            unchecked // unchecked
            {
                x++;
            }

            switch (x) // switch
            {
                case 1:
                    return 1;
            }

            if (x == 3) /* first */ // second
            {
                x++;
            }

            if (x == 4) // empty block
            {
            }

            if (x == 5) // before a comment inside
            {
                // already inside
                x++;
            }

            return x;
        }

        public void NotReported(bool a, int[] items)
        {
            // above the statement
            if (a)
            ////if (!a)
            {
                // inside
            }

            if (a)
            { // after the brace
            }

            if (/* inside the condition */ a)
            {
            }

            if (a) /// three slashes: a documentation comment when docs are parsed
            {
            }

            using (var d = new System.IO.MemoryStream()) // using
            {
            }

            foreach (var (x, y) in new[] { (1, 2) }) // deconstructing foreach
            {
            }

            { // plain block
            }

            Action f = () => // lambda
            {
            };

            if (a) // no block
                f();
        }

        public void Local() // method body
        {
            void Inner() // local function
            {
            }

            Inner();
        }

        public void Skipped(bool a)
        {
            if (a) // single-line block
            { a = !a; }

            if (a) /* spans
                lines */
            {
            }

            if (a
                && !a) // multi-line header
            {
            }
            else if (!a
                || a) // multi-line else if
            {
            }
            else // one-line else after them: reported
            {
            }
        }
    }
}
