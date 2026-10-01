using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Documentation.DocumentationAnalyzer, StyleBro.CodeFixes.Documentation.DocumentationCodeFixProvider>;

namespace StyleBro.Tests;

public class DocumentationTests
{
    [Fact]
    public Task OverridesAndImplementations_GetInheritDoc() => VerifyFixAsync("""
        using System;

        /// <summary>A shape.</summary>
        public interface IShape
        {
            /// <summary>Gets the area.</summary>
            /// <returns>The area.</returns>
            double Area();

            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Raised on change.</summary>
            event EventHandler Changed;

            /// <summary>Gets an item.</summary>
            /// <param name="i">The index.</param>
            int this[int i] { get; }
        }

        /// <summary>A base.</summary>
        public abstract class Base
        {
            /// <summary>Draws.</summary>
            public abstract void Draw();
        }

        /// <summary>A circle.</summary>
        public class Circle : Base, IShape, IDisposable
        {
            public string {|BRO1601:Name|} => "circle";

            public event EventHandler {|BRO1601:Changed|};

            public int {|BRO1601:this|}[int i] => i;

            public double {|BRO1601:Area|}() => 1;

            [Obsolete]
            public override void {|BRO1601:Draw|}()
            {
            }

            void IDisposable.{|BRO1601:Dispose|}()
            {
            }

            // Just a comment.
            public override string {|BRO1601:ToString|}() => Name;

            /// <summary>Documented.</summary>
            public override int GetHashCode() => 1;

            public void NotAnOverride()
            {
            }

            private class Private : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
        """, """
        using System;

        /// <summary>A shape.</summary>
        public interface IShape
        {
            /// <summary>Gets the area.</summary>
            /// <returns>The area.</returns>
            double Area();

            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Raised on change.</summary>
            event EventHandler Changed;

            /// <summary>Gets an item.</summary>
            /// <param name="i">The index.</param>
            int this[int i] { get; }
        }

        /// <summary>A base.</summary>
        public abstract class Base
        {
            /// <summary>Draws.</summary>
            public abstract void Draw();
        }

        /// <summary>A circle.</summary>
        public class Circle : Base, IShape, IDisposable
        {
            /// <inheritdoc/>
            public string Name => "circle";

            /// <inheritdoc/>
            public event EventHandler Changed;

            /// <inheritdoc/>
            public int this[int i] => i;

            /// <inheritdoc/>
            public double Area() => 1;

            /// <inheritdoc/>
            [Obsolete]
            public override void Draw()
            {
            }

            /// <inheritdoc/>
            void IDisposable.Dispose()
            {
            }

            // Just a comment.
            /// <inheritdoc/>
            public override string ToString() => Name;

            /// <summary>Documented.</summary>
            public override int GetHashCode() => 1;

            public void NotAnOverride()
            {
            }

            private class Private : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
        """);

    [Fact]
    public Task TripleSlashComments_BecomePlainComments() => VerifyFixAsync("""
        class C
        {
            void M()
            {
                {|BRO1602:/// Explains the next line.|}
                var x = 1;
                {|BRO1602:///tight|}
                var y = 2;
                //// Commented-out code stays.
            }
        }
        """, """
        class C
        {
            void M()
            {
                // Explains the next line.
                var x = 1;
                // tight
                var y = 2;
                //// Commented-out code stays.
            }
        }
        """);

    [Fact]
    public Task DocumentationText_EndsWithAPeriod() => VerifyFixAsync("""
        using System;

        /// <summary>A class without a period{|BRO1603:|}</summary>
        public class Texts
        {
            /// <summary>
            /// Multi-line summary without a period{|BRO1603:|}
            /// </summary>
            public void A()
            {
            }

            /// <summary>Ends with a see <see cref="Texts"/>{|BRO1603:|}</summary>
            public void B()
            {
            }

            /// <summary>Has a trailing space{|BRO1603:|} </summary>
            /// <param name="value">The value{|BRO1603:|}</param>
            /// <returns>The result{|BRO1603:|}</returns>
            /// <exception cref="ArgumentException">When bad{|BRO1603:|}</exception>
            public int M(int value) => value;

            /// <remarks>Ends with a quote "done"{|BRO1603:|}</remarks>
            /// <summary>Has a nested remark.
            /// <remarks>Without a period{|BRO1603:|}</remarks></summary>
            /// <value>Number 42{|BRO1603:|}</value>
            public void C()
            {
            }
        }
        """, """
        using System;

        /// <summary>A class without a period.</summary>
        public class Texts
        {
            /// <summary>
            /// Multi-line summary without a period.
            /// </summary>
            public void A()
            {
            }

            /// <summary>Ends with a see <see cref="Texts"/>.</summary>
            public void B()
            {
            }

            /// <summary>Has a trailing space. </summary>
            /// <param name="value">The value.</param>
            /// <returns>The result.</returns>
            /// <exception cref="ArgumentException">When bad.</exception>
            public int M(int value) => value;

            /// <remarks>Ends with a quote "done".</remarks>
            /// <summary>Has a nested remark.
            /// <remarks>Without a period.</remarks></summary>
            /// <value>Number 42.</value>
            public void C()
            {
            }
        }
        """);

    [Fact]
    public Task FinishedSentencesAndBlocks_AreNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Ends with a period.</summary>
        public class Texts
        {
            /// <summary>Is this a question?</summary>
            public void A()
            {
            }

            /// <summary>Watch out!</summary>
            /// <param name="x">The x.
            /// <remarks>Must be positive.</remarks></param>
            public void B(int x)
            {
            }

            /// <summary>Introduces a list:
            /// <list type="bullet">
            /// <item><description>one</description></item>
            /// </list>
            /// </summary>
            public void C()
            {
            }

            /// <summary>
            /// Ends with code:
            /// <code>
            /// var x = 1;
            /// </code>
            /// </summary>
            public void D()
            {
            }

            /// <summary><inheritdoc cref="A" path="/summary"/></summary>
            /// <param name="x"><inheritdoc cref="A"/></param>
            public void F(int x)
            {
            }

            /// <summary></summary>
            public void E()
            {
            }

            /// <inheritdoc/>
            public override string ToString() => string.Empty;
        }
        """);

    [Fact]
    public Task PropertySummaries_MatchTheAccessors() => VerifyFixAsync("""
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>The name.</summary>
            public string {|BRO1604:Name|} { get; set; }

            /// <summary>Gets the size.</summary>
            public int {|BRO1604:Size|} { get; set; }

            /// <summary>Gets or sets the count.</summary>
            public int {|BRO1604:Count|} { get; }

            /// <summary>Sets the value.</summary>
            public int {|BRO1604:Value|} { get; set; }

            /// <summary>Gets or sets the private thing.</summary>
            public int {|BRO1605:PrivateSet|} { get; private set; }

            /// <summary>Gets or sets the internal thing.</summary>
            public int {|BRO1605:InternalSet|} { get; internal set; }

            /// <summary>Is it closed.</summary>
            public bool {|BRO1604:IsClosed|} { get; set; }

            /// <summary>Whether it is paid.</summary>
            public bool {|BRO1604:IsPaid|} { get; set; }

            /// <summary>Indicates whether it is sent.</summary>
            public bool {|BRO1604:IsSent|} { get; }

            /// <summary>
            /// The URL.
            /// </summary>
            public string {|BRO1604:Url|} => "x";

            /// <summary>URL of the site.</summary>
            public string {|BRO1604:Site|} => "x";
        }
        """, """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Gets or sets the name.</summary>
            public string Name { get; set; }

            /// <summary>Gets or sets the size.</summary>
            public int Size { get; set; }

            /// <summary>Gets the count.</summary>
            public int Count { get; }

            /// <summary>Gets or sets the value.</summary>
            public int Value { get; set; }

            /// <summary>Gets the private thing.</summary>
            public int PrivateSet { get; private set; }

            /// <summary>Gets the internal thing.</summary>
            public int InternalSet { get; internal set; }

            /// <summary>Gets or sets is it closed.</summary>
            public bool IsClosed { get; set; }

            /// <summary>Gets or sets a value indicating whether it is paid.</summary>
            public bool IsPaid { get; set; }

            /// <summary>Gets a value indicating whether it is sent.</summary>
            public bool IsSent { get; }

            /// <summary>
            /// Gets the URL.
            /// </summary>
            public string Url => "x";

            /// <summary>Gets URL of the site.</summary>
            public string Site => "x";
        }
        """);

    [Fact]
    public Task RightPropertySummaries_AreNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Gets or sets the name.</summary>
            public string Name { get; set; }

            /// <summary>Gets or sets the protected thing.</summary>
            public int ProtectedSet { get; protected set; }

            /// <summary>Gets or sets the protected internal thing.</summary>
            public int ProtectedInternalSet { get; protected internal set; }

            /// <summary>Gets a value indicating whether it is open.</summary>
            public bool IsOpen { get; }

            /// <summary>Gets the return value condition.</summary>
            public bool ReturnValue { get; }

            /// <summary>Sets only.</summary>
            public int WriteOnly { set { } }

            /// <summary><see cref="Words"/> count.</summary>
            public int Count { get; set; }

            /// <summary>Gets or sets the id.</summary>
            public int Id { get; init; }

            /// <summary>Gets an item.</summary>
            /// <param name="i">The index.</param>
            public int this[int i] => i;
        }

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
        }
        """);

    [Fact]
    public Task InheritDoc_FollowsTheDocumentationScope() => VerifyFixAsync("""
        public interface IShape
        {
            int Sides { get; }
        }

        /// <summary>Public.</summary>
        public class Square : IShape
        {
            public int {|BRO1601:Sides|} => 4;
        }

        internal class Triangle : IShape
        {
            public int Sides => 3;
        }
        """, """
        public interface IShape
        {
            int Sides { get; }
        }

        /// <summary>Public.</summary>
        public class Square : IShape
        {
            /// <inheritdoc/>
            public int Sides => 4;
        }

        internal class Triangle : IShape
        {
            public int Sides => 3;
        }
        """, editorConfig: "stylebro_document_internal_elements = false");

    [Fact]
    public Task ConstructorSummaries_BeginWithTheStandardText() => VerifyFixAsync("""
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
            public Words()
            {
            }

            /// {|BRO1606:<summary>|}Creates a words object.</summary>
            /// <param name="x">The x.</param>
            public Words(int x)
            {
            }

            /// {|BRO1606:<summary>|}Initializes a new instance of the Words class.</summary>
            /// <param name="s">The s.</param>
            public Words(string s)
            {
            }

            /// {|BRO1606:<summary>|}Initializes a new instance of Words with the given name.</summary>
            /// <param name="b">The b.</param>
            public Words(byte b)
            {
            }

            /// {|BRO1606:<summary>|}
            /// Loads the defaults.
            /// </summary>
            static Words()
            {
            }

            /// {|BRO1606:<summary>|}</summary>
            /// <param name="d">The d.</param>
            private Words(double d)
            {
            }

            /// {|BRO1607:<summary>|}Cleans up.</summary>
            ~Words()
            {
            }
        }

        /// <summary>A point.</summary>
        /// <typeparam name="T">The type.</typeparam>
        public struct Point<T>
        {
            /// {|BRO1606:<summary>|}Makes a point.</summary>
            /// <param name="x">The x.</param>
            public Point(int x)
            {
            }
        }
        """, """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
            public Words()
            {
            }

            /// <summary>Initializes a new instance of the <see cref="Words"/> class. Creates a words object.</summary>
            /// <param name="x">The x.</param>
            public Words(int x)
            {
            }

            /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
            /// <param name="s">The s.</param>
            public Words(string s)
            {
            }

            /// <summary>Initializes a new instance of the <see cref="Words"/> class. Initializes a new instance of Words with the given name.</summary>
            /// <param name="b">The b.</param>
            public Words(byte b)
            {
            }

            /// <summary>
            /// Initializes static members of the <see cref="Words"/> class. Loads the defaults.
            /// </summary>
            static Words()
            {
            }

            /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
            /// <param name="d">The d.</param>
            private Words(double d)
            {
            }

            /// <summary>Finalizes an instance of the <see cref="Words"/> class. Cleans up.</summary>
            ~Words()
            {
            }
        }

        /// <summary>A point.</summary>
        /// <typeparam name="T">The type.</typeparam>
        public struct Point<T>
        {
            /// <summary>Initializes a new instance of the <see cref="Point{T}"/> struct. Makes a point.</summary>
            /// <param name="x">The x.</param>
            public Point(int x)
            {
            }
        }
        """);

    [Fact]
    public Task StandardConstructorSummaries_AreNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Prevents a default instance of the <see cref="Words" /> class from being created.</summary>
            private Words()
            {
            }

            /// <inheritdoc/>
            public Words(int x)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Words"/> class representing <paramref name="b"/>.
            /// </summary>
            /// <param name="b">The b.</param>
            public Words(byte b)
            {
            }

            public Words(string s)
            {
            }
        }

        /// <summary>A pair.</summary>
        /// <typeparam name="TKey">The key.</typeparam>
        /// <typeparam name="TValue">The value.</typeparam>
        public class Pair<TKey, TValue>
        {
            /// <summary>Initializes a new instance of the <see cref="Pair{TKey, TValue}" /> class.</summary>
            public Pair()
            {
            }
        }
        """);

    [Fact]
    public Task VoidReturnsAndPlaceholders_AreRemoved() => VerifyFixAsync("""
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Does nothing.</summary>
            /// {|BRO1608:<returns>Nothing.</returns>|}
            public void Nothing()
            {
            }

            /// <summary>Also nothing.</summary> {|BRO1608:<returns>Nothing.</returns>|}
            public void Inline()
            {
            }

            /// <summary>Returns a number.</summary>
            /// <returns>A number.</returns>
            public int Number() => 1;

            /// <summary>Handles it.</summary>
            /// {|BRO1608:<returns>Nothing.</returns>|}
            public delegate void Handler();

            /// <summary>{|BRO1609:<placeholder>|}Fills the cache.</placeholder></summary>
            public void Fill()
            {
            }
        }
        """, """
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Does nothing.</summary>
            public void Nothing()
            {
            }

            /// <summary>Also nothing.</summary>
            public void Inline()
            {
            }

            /// <summary>Returns a number.</summary>
            /// <returns>A number.</returns>
            public int Number() => 1;

            /// <summary>Handles it.</summary>
            public delegate void Handler();

            /// <summary>Fills the cache.</summary>
            public void Fill()
            {
            }
        }
        """);

    [Fact]
    public Task EmptyRemarksAndStaleParameterTags_AreFixed() => VerifyFixAsync("""
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Empty remarks.</summary>
            /// {|BRO1610:<remarks></remarks>|}
            public void EmptyRemarks()
            {
            }

            /// <summary>Adds.</summary>
            /// <param name="a">The a.</param>
            /// <param name="{|BRO1611:old|}">Removed.</param>
            /// <param name="b">The b.</param>
            /// <param name="{|BRO1611:c|}">The c.</param>
            public void Removed(int a, int b)
            {
            }

            /// <summary>Renamed.</summary>
            /// <param name="{|BRO1611:value|}">The input.</param>
            public void Renamed(int input)
            {
            }

            /// <summary>Swapped.</summary>
            /// <param name="{|BRO1611:b|}">The b.</param>
            /// <param name="{|BRO1611:a|}">The a.</param>
            public void Swapped(int a, int b)
            {
            }
        }
        """, """
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Empty remarks.</summary>
            public void EmptyRemarks()
            {
            }

            /// <summary>Adds.</summary>
            /// <param name="a">The a.</param>
            /// <param name="b">The b.</param>
            public void Removed(int a, int b)
            {
            }

            /// <summary>Renamed.</summary>
            /// <param name="input">The input.</param>
            public void Renamed(int input)
            {
            }

            /// <summary>Swapped.</summary>
            /// <param name="a">The a.</param>
            /// <param name="b">The b.</param>
            public void Swapped(int a, int b)
            {
            }
        }
        """);

    [Fact]
    public Task ParameterTagsThatCantBeFixed_AreNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Shares a line.</summary>
            /// <param name="b">The b.</param> <param name="a">The a.</param>
            public void SharedLine(int a, int b)
            {
            }

            /// <summary>Duplicate.</summary>
            /// <param name="a">The a.</param>
            /// <param name="a">The a again.</param>
            /// <param name="old">Old.</param>
            public void Duplicate(int a, int b)
            {
            }
        }
        """);

    [Fact]
    public Task UnnamedParamTags_GetTheirName() => VerifyFixAsync("""
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>One.</summary>
            /// {|BRO1612:<param>|}The a.</param>
            public void One(int a)
            {
            }

            /// <summary>Second.</summary>
            /// <param name="a">The a.</param>
            /// {|BRO1612:<param>|}The b.</param>
            public void Second(int a, int b)
            {
            }

            /// <summary>All unnamed.</summary>
            /// {|BRO1612:<param>|}The a.</param>
            /// {|BRO1612:<param>|}The b.</param>
            public void All(int a, int b)
            {
            }

            /// <summary>Empty name.</summary>
            /// <param {|BRO1612:name=""|}>The a.</param>
            public void Empty(int a)
            {
            }
        }
        """, """
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>One.</summary>
            /// <param name="a">The a.</param>
            public void One(int a)
            {
            }

            /// <summary>Second.</summary>
            /// <param name="a">The a.</param>
            /// <param name="b">The b.</param>
            public void Second(int a, int b)
            {
            }

            /// <summary>All unnamed.</summary>
            /// <param name="a">The a.</param>
            /// <param name="b">The b.</param>
            public void All(int a, int b)
            {
            }

            /// <summary>Empty name.</summary>
            /// <param name="a">The a.</param>
            public void Empty(int a)
            {
            }
        }
        """);

    [Fact]
    public Task AmbiguousUnnamedTags_AreNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Tags.</summary>
        public class Tags
        {
            /// <summary>Two unnamed, three parameters.</summary>
            /// <param>The a.</param>
            /// <param>The b.</param>
            public void Unclear(int a, int b, int c)
            {
            }

            /// <summary>Unnamed next to a stale tag.</summary>
            /// <param name="old">The old.</param>
            /// <param>The b.</param>
            public void Stale(int a, int b)
            {
            }

            /// <summary>Unnamed before a stale tag.</summary>
            /// <param>The a.</param>
            /// <param name="old">The old.</param>
            public void StaleAfter(int a, int b)
            {
            }
        }
        """);

    [Fact]
    public Task TypeParamTags_MatchTheTypeParameters() => VerifyFixAsync("""
        /// <summary>A box.</summary>
        /// <typeparam name="{|BRO1613:TOld|}">The type.</typeparam>
        public class Box<T>
        {
            /// <summary>Swapped.</summary>
            /// <typeparam name="{|BRO1613:TValue|}">The value.</typeparam>
            /// <typeparam name="{|BRO1613:TKey|}">The key.</typeparam>
            public void Swapped<TKey, TValue>()
            {
            }

            /// <summary>Extra.</summary>
            /// <typeparam name="TItem">The item.</typeparam>
            /// <typeparam name="{|BRO1613:TGone|}">Gone.</typeparam>
            public void Extra<TItem>()
            {
            }
        }

        /// <summary>Unnamed.</summary>
        /// {|BRO1614:<typeparam>|}The type.</typeparam>
        public struct Holder<T>
        {
        }
        """, """
        /// <summary>A box.</summary>
        /// <typeparam name="T">The type.</typeparam>
        public class Box<T>
        {
            /// <summary>Swapped.</summary>
            /// <typeparam name="TKey">The key.</typeparam>
            /// <typeparam name="TValue">The value.</typeparam>
            public void Swapped<TKey, TValue>()
            {
            }

            /// <summary>Extra.</summary>
            /// <typeparam name="TItem">The item.</typeparam>
            public void Extra<TItem>()
            {
            }
        }

        /// <summary>Unnamed.</summary>
        /// <typeparam name="T">The type.</typeparam>
        public struct Holder<T>
        {
        }
        """);
}
