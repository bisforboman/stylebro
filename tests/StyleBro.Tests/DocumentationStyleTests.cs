using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Documentation.DocumentationStyleAnalyzer, StyleBro.CodeFixes.Documentation.DocumentationStyleCodeFixProvider>;

namespace StyleBro.Tests;

public class DocumentationStyleTests
{
    [Fact]
    public Task GenericCref_EntitiesBecomeBraces() => VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;

        /// <summary>Uses <see cref="{|BRO1617:List&lt;T&gt;|}"/> and <seealso cref="{|BRO1617:Dictionary&lt;TKey, TValue&gt;|}"/>.</summary>
        public class C
        {
            /// <inheritdoc cref="{|BRO1617:M&lt;T&gt;(List&lt;List&lt;T&gt;&gt;)|}"/>
            public void N()
            {
            }

            /// <summary>Does it.</summary>
            /// <exception cref="{|BRO1617:Action&lt;T&gt;|}">Never.</exception>
            public void M<T>(List<List<T>> list)
            {
            }
        }
        """,
        """
        using System;
        using System.Collections.Generic;

        /// <summary>Uses <see cref="List{T}"/> and <seealso cref="Dictionary{TKey, TValue}"/>.</summary>
        public class C
        {
            /// <inheritdoc cref="M{T}(List{List{T}})"/>
            public void N()
            {
            }

            /// <summary>Does it.</summary>
            /// <exception cref="Action{T}">Never.</exception>
            public void M<T>(List<List<T>> list)
            {
            }
        }
        """);

    [Fact]
    public Task GenericCref_NotReported() => VerifyNoDiagnosticsAsync(
        """
        using System.Collections.Generic;

        /// <summary>
        /// Already braces: <see cref="List{T}"/>; an error: <see cref="List&lt;List&lt;int&gt;&gt;"/>; an operator: <see cref="C.op_LessThan"/>, <see cref="operator &lt;(C, C)"/>;
        /// text, not a cref: List&lt;T&gt;; in code: <code><see cref="List&lt;T&gt;"/></code>.
        /// </summary>
        public class C
        {
            /// <summary>Compares.</summary>
            /// <param name="a">A.</param>
            /// <param name="b">B.</param>
            /// <returns>Whether.</returns>
            public static bool operator <(C a, C b) => false;

            /// <summary>Compares.</summary>
            /// <param name="a">A.</param>
            /// <param name="b">B.</param>
            /// <returns>Whether.</returns>
            public static bool operator >(C a, C b) => false;
        }
        """);

    [Fact]
    public Task Langword_KeywordsInC() => VerifyFixAsync(
        """
        /// <summary>
        /// Returns {|BRO1618:<c>null</c>|} or {|BRO1618:<c>true</c>|}; never {|BRO1618:<c>false</c>|}.
        /// </summary>
        /// <remarks>
        /// <para>Use {|BRO1618:<c>await</c>|} on {|BRO1618:<c>static</c>|} members.</para>
        /// </remarks>
        public class C
        {
        }
        """,
        """
        /// <summary>
        /// Returns <see langword="null"/> or <see langword="true"/>; never <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>Use <see langword="await"/> on <see langword="static"/> members.</para>
        /// </remarks>
        public class C
        {
        }
        """);

    [Fact]
    public Task Langword_NotReported() => VerifyNoDiagnosticsAsync(
        """
        /// <summary>
        /// Not keywords: <c>Null</c>, <c>value</c>, <c>get</c>, <c>__arglist</c>; more than a keyword: <c>null!</c>,
        /// <c> null </c>, <c>x is null</c>, <c>null</C>, <C>null</c>, <c>null&amp;&amp;</c>, <c>null<b>x</b></c>; an attribute: <c lang="cs">null</c>; in code:
        /// <code>null</code>, <code><c>null</c></code>; not <c>: <i>null</i>; already: <see langword="null"/>.
        /// </summary>
        public class C
        {
        }
        """);

    [Fact]
    public Task ElementOrder_SortsTheTopLevelElements() => VerifyFixAsync(
        """
        public class C
        {
            /// {|BRO1619:<returns>|}A number.</returns>
            /// <param name="b">B.</param>
            /// <exception cref="System.Exception">
            /// Sometimes.
            /// </exception>
            /// <summary>
            /// Adds.
            /// </summary>
            ///
            /// <param name="a">A.</param>
            /// <typeparam name="T">T.</typeparam>
            /// <seealso cref="C"/>
            /// <remarks>Fast.</remarks>
            public int M<T>(int b, int a) => a + b;
        }
        """,
        """
        public class C
        {
            /// <summary>
            /// Adds.
            /// </summary>
            /// <typeparam name="T">T.</typeparam>
            /// <param name="b">B.</param>
            /// <param name="a">A.</param>
            ///
            /// <returns>A number.</returns>
            /// <exception cref="System.Exception">
            /// Sometimes.
            /// </exception>
            /// <remarks>Fast.</remarks>
            /// <seealso cref="C"/>
            public int M<T>(int b, int a) => a + b;
        }
        """);

    [Fact]
    public Task ElementOrder_NotReported() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            /// <summary>In order.</summary>
            /// <param name="a">A.</param>
            /// <returns>A.</returns>
            public int A(int a) => a;

            /// <returns>Shares a line.</returns> <summary>B.</summary>
            public int B() => 1;

            /// <returns>An element of its own.</returns>
            /// <summary>Unknown element: <c>x</c>.</summary>
            /// <custom>Kept where it is.</custom>
            public int D() => 1;

            /// <returns>Text outside the elements.</returns>
            /// Loose text.
            /// <summary>E.</summary>
            public int E() => 1;

            /// <returns>Inherited.</returns>
            /// <inheritdoc/>
            public int F() => 1;

            /** <returns>A block comment.</returns>
                <summary>G.</summary> */
            public int G() => 1;

            /// <returns>Ends on
            /// a line with another element.</returns> <summary>H.</summary>
            public int H() => 1;
        }
        """);
}
