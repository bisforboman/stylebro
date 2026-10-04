namespace Probe
{
    using System;

    /// <summary>A class without a period</summary>
    public class Texts
    {
        /// <summary>
        /// Multi-line summary without a period
        /// </summary>
        public int A { get; set; }

        /// <summary>Ends with a period.</summary>
        public int B { get; set; }

        /// <summary>Ends with a question?</summary>
        public int C { get; set; }

        /// <summary>Ends with a see <see cref="Texts"/></summary>
        public int D { get; set; }

        /// <summary>Ends with code <c>x</c></summary>
        public int E { get; set; }

        /// <summary>The name</summary>
        /// <param name="value">The value</param>
        /// <returns>The result</returns>
        public int M(int value) => value;

        /// <summary>A list:
        /// <list type="bullet">
        /// <item><description>one</description></item>
        /// </list>
        /// </summary>
        public int F { get; set; }

        /// <summary>Ends with a colon:</summary>
        public int G { get; set; }

        /// <summary>Ends with a closing paren (see above)</summary>
        public int H { get; set; }

        /// <summary>Ends with a period in parens (see above.)</summary>
        public int H2 { get; set; }

        /// <summary>Ends with a quoted "sentence."</summary>
        public int H3 { get; set; }

        /// <summary>Ends with an entity List&lt;T&gt;</summary>
        public int H4 { get; set; }

        /// <summary>Ends with an exclamation!</summary>
        public int I { get; set; }

        /// <remarks>Remarks without period</remarks>
        public int J { get; set; }

        /// <summary>Has a trailing space </summary>
        public int K { get; set; }

        /// <summary>
        /// Has code at the end:
        /// <code>
        /// var x = 1;
        /// </code>
        /// </summary>
        public int L { get; set; }

        /// <summary>Value doc</summary>
        /// <value>The value</value>
        public int N { get; set; }

        /// <summary>Gets something</summary>
        /// <exception cref="ArgumentException">When bad</exception>
        public int O { get; set; }

        /// <summary>Ends with a quote "done"</summary>
        public int P { get; set; }

        /// <summary>Number 42</summary>
        public int Q { get; set; }
    }
}
