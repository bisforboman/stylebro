using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Documentation;
using StyleBro.CodeFixes.Documentation;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Documentation.DocumentationAnalyzer, StyleBro.CodeFixes.Documentation.DocumentationCodeFixProvider>;

namespace StyleBro.Tests;

public class DocumentationTests
{
    // Two single-line properties may sit together (BRO1505), but a documented one wants a blank line above its docs
    // (BRO1513): the inserted '<inheritdoc/>' brings it along, so 'dotnet format' converges in one run (FFMpegCore).
    [Fact]
    public Task InheritDoc_BelowCode_GetsTheBlankLineBRO1513Wants() => VerifyFixAsync(
        """
        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            /// <summary>Gets the size.</summary>
            public int Size { get; }
            public string {|BRO1601:Name|} => "thing";
        }
        """,
        """
        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            /// <summary>Gets the size.</summary>
            public int Size { get; }

            /// <inheritdoc/>
            public string Name => "thing";
        }
        """);

    // Like StyleCop's unreleased master (2959cac8): an explicit implementation is only reachable through the interface,
    // whose documentation tools show.
    [Fact]
    public Task ExplicitInterfaceImplementations_AreNotReported() => VerifyFixAsync(
        """
        using System;

        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Raised on change.</summary>
            event EventHandler Changed;

            /// <summary>Runs.</summary>
            void Run();
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            string IThing.Name => "thing";

            event EventHandler IThing.Changed
            {
                add { }
                remove { }
            }

            void IThing.Run()
            {
            }
        }
        """,
        """
        using System;

        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Raised on change.</summary>
            event EventHandler Changed;

            /// <summary>Runs.</summary>
            void Run();
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            string IThing.Name => "thing";

            event EventHandler IThing.Changed
            {
                add { }
                remove { }
            }

            void IThing.Run()
            {
            }
        }
        """);

    [Fact]
    public Task OverridesAndImplementations_GetInheritDoc() => VerifyFixAsync(
        """
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

            void IDisposable.Dispose()
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
        """,
        """
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
    public Task TripleSlashComments_BecomePlainComments() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task DocumentationText_EndsWithAPeriod() => VerifyFixAsync(
        """
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
        """,
        """
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

            /// <summary>Has a trailing space.</summary>
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
    public Task PeriodBeforeClosingPunctuation_IsNotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>Gets the list (see above.)</summary>
        public class Texts
        {
            /// <summary>Gets the word "done."</summary>
            public void A()
            {
            }

            /// <summary>Gets the word 'done.'</summary>
            /// <remarks>Ends in brackets [like this.]</remarks>
            public void B()
            {
            }

            /// <summary>Gets the word &quot;done.&quot;</summary>
            public void C()
            {
            }
        }
        """);

    // Ocelot's FileAggregateRoute: a documented property is set apart from the property below it too (BRO1505), so the
    // result doesn't depend on whether BRO1601 runs before BRO1001's sort.
    [Fact]
    public Task InheritDoc_OnAdjacentProperties_SeparatesThem() => VerifyFixAsync(
        """
        public interface IRoute
        {
            /// <summary>Gets the priority.</summary>
            int Priority { get; }

            /// <summary>Gets the host.</summary>
            string Host { get; }
        }

        public class Route : IRoute
        {
            public string Name { get; set; }
            public int {|BRO1601:Priority|} { get; set; }
            public string {|BRO1601:Host|} { get; set; }

            public string Path { get; set; }
        }
        """,
        """
        public interface IRoute
        {
            /// <summary>Gets the priority.</summary>
            int Priority { get; }

            /// <summary>Gets the host.</summary>
            string Host { get; }
        }

        public class Route : IRoute
        {
            public string Name { get; set; }

            /// <inheritdoc/>
            public int Priority { get; set; }

            /// <inheritdoc/>
            public string Host { get; set; }

            public string Path { get; set; }
        }
        """);

    // vs-threading's Types.cs: StyleCop's SA1629 passes over elements it doesn't know, and checks the text before them.
    [Fact]
    public Task UnknownElements_ArePassedOver() => VerifyFixAsync(
        """
        /// <summary>
        /// Identifiers used to identify types.
        /// <devremarks>For each value here, please update the unit test</devremarks>
        /// </summary>
        public class Types
        {
            /// <summary>Gets the value{|BRO1603:|} <devremarks>internal</devremarks></summary>
            public int A { get; }

            /// <summary>Gets the value{|BRO1603:|}<para/></summary>
            public int B { get; }

            /// <summary>Returns <b>true</b>{|BRO1603:|}</summary>
            public bool C() => true;
        }
        """,
        """
        /// <summary>
        /// Identifiers used to identify types.
        /// <devremarks>For each value here, please update the unit test</devremarks>
        /// </summary>
        public class Types
        {
            /// <summary>Gets the value. <devremarks>internal</devremarks></summary>
            public int A { get; }

            /// <summary>Gets the value.<para/></summary>
            public int B { get; }

            /// <summary>Returns <b>true</b>.</summary>
            public bool C() => true;
        }
        """);

    [Fact]
    public Task QuotedSentenceInATrailingCodeElement_IsNotReported() => VerifyFixAsync(
        """
        public class Logs
        {
            /// <summary>Writes a message.</summary>
            /// <param name="message">Example: <c>"User {User} logged in."</c></param>
            /// <param name="other">Example: <c>'Done.'</c></param>
            /// <param name="third">Example: <c>&quot;Done.&quot;</c></param>
            /// <param name="fourth">Example: <c>"Not a sentence"</c>{|BRO1603:|}</param>
            /// <param name="fifth">Calls <c>Done.</c>{|BRO1603:|}</param>
            public void Write(string message, string other, string third, string fourth, string fifth)
            {
            }
        }
        """,
        """
        public class Logs
        {
            /// <summary>Writes a message.</summary>
            /// <param name="message">Example: <c>"User {User} logged in."</c></param>
            /// <param name="other">Example: <c>'Done.'</c></param>
            /// <param name="third">Example: <c>&quot;Done.&quot;</c></param>
            /// <param name="fourth">Example: <c>"Not a sentence"</c>.</param>
            /// <param name="fifth">Calls <c>Done.</c>.</param>
            public void Write(string message, string other, string third, string fourth, string fifth)
            {
            }
        }
        """);

    [Fact]
    public Task TextEndingInAnEntity_GetsThePeriodAfterIt() => VerifyFixAsync(
        """
        /// <summary>Gets the List&lt;T&gt;{|BRO1603:|}</summary>
        public class Texts
        {
            /// <summary>Gets a closing parenthesis ({|BRO1603:|}</summary>
            /// <remarks>Gets the word &quot;done&quot;{|BRO1603:|}</remarks>
            public void A()
            {
            }
        }
        """,
        """
        /// <summary>Gets the List&lt;T&gt;.</summary>
        public class Texts
        {
            /// <summary>Gets a closing parenthesis (.</summary>
            /// <remarks>Gets the word &quot;done&quot;.</remarks>
            public void A()
            {
            }
        }
        """);

    [Fact]
    public Task ExcludedTags_AreNotCheckedForPeriods() => VerifyFixAsync(
        """
        /// <summary>Not checked</summary>
        /// <remarks>Checked{|BRO1603:|}</remarks>
        public class Texts
        {
            /// <remarks>Has a paragraph.
            /// <para>Not checked either</para></remarks>
            public void A()
            {
            }
        }
        """,
        """
        /// <summary>Not checked</summary>
        /// <remarks>Checked.</remarks>
        public class Texts
        {
            /// <remarks>Has a paragraph.
            /// <para>Not checked either</para></remarks>
            public void A()
            {
            }
        }
        """,
        "stylebro_exclude_from_punctuation_check = summary, para\n");

    [Fact]
    public Task PropertySummaries_MatchTheAccessors() => VerifyFixAsync(
        """
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
            public bool IsClosed { get; set; }

            /// <summary>Whether it is paid.</summary>
            public bool {|BRO1604:IsPaid|} { get; set; }

            /// <summary>Indicates whether it is sent.</summary>
            public bool {|BRO1604:IsSent|} { get; }

            /// <summary>
            /// The URL.
            /// </summary>
            public string {|BRO1604:Url|} => "x";

            /// <summary>Sets URL of the site.</summary>
            public string {|BRO1604:Site|} => "x";

            /// <summary>An id.</summary>
            public int {|BRO1604:Id|} { get; set; }

            /// <summary>A key.</summary>
            public int {|BRO1604:Key|} { get; }

            /// <summary>number of pages.</summary>
            public int {|BRO1604:Pages|} { get; }
        }
        """,
        """
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

            /// <summary>Is it closed.</summary>
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

            /// <summary>Gets or sets an id.</summary>
            public int Id { get; set; }

            /// <summary>Gets a key.</summary>
            public int Key { get; }

            /// <summary>Gets number of pages.</summary>
            public int Pages { get; }
        }
        """);

    // Like StyleCop master (eb498962): 'init' counts as a setter that says 'initializes'; 'Gets' alone is fine for
    // get+init; 'Gets or initializes' is a wrong prefix elsewhere (beta.556's fix wrote 'Gets or sets or initializes').
    [Fact]
    public Task InitAccessors_SayInitializes() => VerifyFixAsync(
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Gets or sets the id.</summary>
            public int {|BRO1604:Id|} { get; init; }

            /// <summary>The name.</summary>
            public string {|BRO1604:Name|} { get; init; }

            /// <summary>Sets the seed.</summary>
            public int {|BRO1604:Seed|} { init { } }

            /// <summary>Gets or initializes the code.</summary>
            public int {|BRO1605:Code|} { get; private init; }

            /// <summary>Whether it is new.</summary>
            public bool {|BRO1604:IsNew|} { get; init; }

            /// <summary>Gets or initializes the value.</summary>
            public int {|BRO1604:Value|} { get; set; }

            /// <summary>Gets or initializes the count.</summary>
            public int {|BRO1604:Count|} { get; }
        }

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
        }
        """,
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>Gets or initializes the id.</summary>
            public int Id { get; init; }

            /// <summary>Gets or initializes the name.</summary>
            public string Name { get; init; }

            /// <summary>Initializes the seed.</summary>
            public int Seed { init { } }

            /// <summary>Gets the code.</summary>
            public int Code { get; private init; }

            /// <summary>Gets or initializes a value indicating whether it is new.</summary>
            public bool IsNew { get; init; }

            /// <summary>Gets or sets the value.</summary>
            public int Value { get; set; }

            /// <summary>Gets the count.</summary>
            public int Count { get; }
        }

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
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

            /// <summary>Gets or initializes the id.</summary>
            public int Id { get; init; }

            /// <summary>Gets the key.</summary>
            public int Key { get; init; }

            /// <summary>Gets a value indicating whether it is new.</summary>
            public bool IsNew { get; init; }

            /// <summary>Initializes the seed.</summary>
            public int Seed { init { } }

            /// <summary>Gets the code.</summary>
            public int Code { get; private init; }

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
    public Task InheritDoc_FollowsTheDocumentationScope() => VerifyFixAsync(
        """
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
        """,
        """
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
        """,
        editorConfig: "stylebro_document_internal_elements = false");

    // Both insertion paths (plain, and below code with BRO1513's blank line) write the configured form; an existing
    // compact tag isn't rewritten.
    [Fact]
    public Task InheritDoc_SpacedStyle_InsertsTheSpacedTag() => VerifyFixAsync(
        """
        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Gets the size.</summary>
            int Size { get; }
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            /// <inheritdoc/>
            public int Size { get; }
            public string {|BRO1601:Name|} => "thing";

            public override string {|BRO1601:ToString|}() => Name;
        }
        """,
        """
        /// <summary>Something.</summary>
        public interface IThing
        {
            /// <summary>Gets the name.</summary>
            string Name { get; }

            /// <summary>Gets the size.</summary>
            int Size { get; }
        }

        /// <summary>A thing.</summary>
        public class Thing : IThing
        {
            /// <inheritdoc/>
            public int Size { get; }

            /// <inheritdoc />
            public string Name => "thing";

            /// <inheritdoc />
            public override string ToString() => Name;
        }
        """,
        editorConfig: "stylebro_inheritdoc_style = spaced");

    [Fact]
    public Task ConstructorSummaries_BeginWithTheStandardText() => VerifyFixAsync(
        """
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

            /// {|BRO1606:<summary>|}Initializes a new instance of the Words class. Initializes the cache.</summary>
            /// <param name="c">The c.</param>
            public Words(char c)
            {
            }

            /// <summary>Initializes a new instance of Words with the given name.</summary>
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

            /// {|BRO1606:<summary>|}  </summary>
            /// <param name="f">The f.</param>
            public Words(float f)
            {
            }

            /// {|BRO1606:<summary>|}
            /// </summary>
            /// <param name="l">The l.</param>
            public Words(long l)
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
        """,
        """
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

            /// <summary>Initializes a new instance of the <see cref="Words"/> class. Initializes the cache.</summary>
            /// <param name="c">The c.</param>
            public Words(char c)
            {
            }

            /// <summary>Initializes a new instance of Words with the given name.</summary>
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

            /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
            /// <param name="f">The f.</param>
            public Words(float f)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Words"/> class.
            /// </summary>
            /// <param name="l">The l.</param>
            public Words(long l)
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

            /** <summary>
                </summary> */
            public Words(long l)
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
    public Task ConstructorSummariesInAParagraph_AreJudgedByTheParagraph() => VerifyFixAsync(
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary><para>Initializes a new instance of the <see cref="Words"/> class.</para></summary>
            public Words()
            {
            }

            /// <summary>
            /// <para>Prevents a default instance of the <see cref="Words"/> class from being created.</para>
            /// <para>Use the factory.</para>
            /// </summary>
            /// <param name="b">The b.</param>
            private Words(byte b)
            {
            }

            /// {|BRO1606:<summary>|}
            /// <para>Creates a words object.</para>
            /// </summary>
            /// <param name="x">The x.</param>
            public Words(int x)
            {
            }
        }
        """,
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary><para>Initializes a new instance of the <see cref="Words"/> class.</para></summary>
            public Words()
            {
            }

            /// <summary>
            /// <para>Prevents a default instance of the <see cref="Words"/> class from being created.</para>
            /// <para>Use the factory.</para>
            /// </summary>
            /// <param name="b">The b.</param>
            private Words(byte b)
            {
            }

            /// <summary>
            /// <para>Initializes a new instance of the <see cref="Words"/> class. Creates a words object.</para>
            /// </summary>
            /// <param name="x">The x.</param>
            public Words(int x)
            {
            }
        }
        """);

    [Fact]
    public Task VoidReturnsAndPlaceholders_AreRemoved() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task EmptyRemarksAndStaleParameterTags_AreFixed() => VerifyFixAsync(
        """
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
        """,
        """
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

    // Like StyleCop master (24dd9011): primary constructor parameters of classes, structs and records.
    [Fact]
    public Task PrimaryConstructorParameterTags_AreChecked() => VerifyFixAsync(
        """
        /// <summary>Renamed.</summary>
        /// <param name="{|BRO1611:value|}">The input.</param>
        public class Renamed(int input)
        {
            /// <summary>Gets the input.</summary>
            public int Input => input;
        }

        /// <summary>Swapped.</summary>
        /// <param name="{|BRO1611:b|}">The b.</param>
        /// <param name="{|BRO1611:a|}">The a.</param>
        public record Swapped(int a, int b);

        /// <summary>Stale.</summary>
        /// <param name="a">The a.</param>
        /// <param name="{|BRO1611:old|}">Old.</param>
        public readonly record struct Stale(int a);

        /// <summary>Struct.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        /// <param name="{|BRO1611:old|}">Old.</param>
        public struct Point(int a, int b)
        {
            /// <summary>Gets the sum.</summary>
            public int Sum => a + b;
        }

        /// <summary>No parameter list: not checked.</summary>
        /// <param name="old">Old.</param>
        public class Plain
        {
        }

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
        }
        """,
        """
        /// <summary>Renamed.</summary>
        /// <param name="input">The input.</param>
        public class Renamed(int input)
        {
            /// <summary>Gets the input.</summary>
            public int Input => input;
        }

        /// <summary>Swapped.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public record Swapped(int a, int b);

        /// <summary>Stale.</summary>
        /// <param name="a">The a.</param>
        public readonly record struct Stale(int a);

        /// <summary>Struct.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public struct Point(int a, int b)
        {
            /// <summary>Gets the sum.</summary>
            public int Sum => a + b;
        }

        /// <summary>No parameter list: not checked.</summary>
        /// <param name="old">Old.</param>
        public class Plain
        {
        }

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
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
    public Task UnnamedParamTags_GetTheirName() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task TypeParamTags_MatchTheTypeParameters() => VerifyFixAsync(
        """
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
        """,
        """
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

    // An '#if' above a member or between its attributes: each target framework's copy wanted '<inheritdoc/>' in another
    // place, and BRO1602 turned the other copy's into '//' (Dapper's WrappedReader: never converged). A '#region' is fine.
    [Fact]
    public Task ConditionalDirectiveAboveTheMember_IsNotReported() => VerifyFixAsync(
        """
        using System;

        /// <summary>A base.</summary>
        public class B : IDisposable
        {
        #if NET5_0_OR_GREATER
            [Obsolete("x")]
        #endif
            public override string ToString() => "b";

            [Obsolete("y")]
        #if NET5_0_OR_GREATER
            [CLSCompliant(false)]
        #endif
            public override int GetHashCode() => 1;

        #if !NET5_0_OR_GREATER
            [Obsolete("z")]
        #endif
            public override bool Equals(object o) => false;

            #region Other
            public void {|BRO1601:Dispose|}()
            {
            }
            #endregion
        }
        """,
        """
        using System;

        /// <summary>A base.</summary>
        public class B : IDisposable
        {
        #if NET5_0_OR_GREATER
            [Obsolete("x")]
        #endif
            public override string ToString() => "b";

            [Obsolete("y")]
        #if NET5_0_OR_GREATER
            [CLSCompliant(false)]
        #endif
            public override int GetHashCode() => 1;

        #if !NET5_0_OR_GREATER
            [Obsolete("z")]
        #endif
            public override bool Equals(object o) => false;

            #region Other

            /// <inheritdoc/>
            public void Dispose()
            {
            }
            #endregion
        }
        """);

    // The owner's decision (docs/decisions.md): no '<inheritdoc/>' where the project generates no documentation, like
    // StyleCop's SA0001. The package passes GenerateDocumentationFile (the same in a build and under 'dotnet format',
    // which parses documentation comments anyway); without it, the compiler's documentation mode decides.
    [Theory]
    [InlineData(null, DocumentationMode.Diagnose, true)]
    [InlineData(null, DocumentationMode.Parse, true)]
    [InlineData(null, DocumentationMode.None, false)]
    [InlineData("true", DocumentationMode.None, true)]
    [InlineData("True", DocumentationMode.None, true)]
    [InlineData("false", DocumentationMode.Diagnose, false)]
    [InlineData("", DocumentationMode.Diagnose, true)]
    public Task InheritDoc_OnlyWhereTheProjectGeneratesDocumentation(string? property, DocumentationMode mode, bool reported)
    {
        var test = new CSharpCodeFixTest<DocumentationAnalyzer, DocumentationCodeFixProvider, DefaultVerifier>
        {
            TestCode = $$"""
                /// <summary>A thing.</summary>
                public class Thing
                {
                    public override string {{(reported ? "{|BRO1601:ToString|}" : "ToString")}}() => "thing";
                }
                """,
            FixedCode = $$"""
                /// <summary>A thing.</summary>
                public class Thing
                {
                    {{(reported ? "/// <inheritdoc/>\n    " : string.Empty)}}public override string ToString() => "thing";
                }
                """,
        };
        if (property is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", $"is_global = true\nbuild_property.GenerateDocumentationFile = {property}\n"));
        }

        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithDocumentationMode(mode)));
        return test.RunAsync();
    }

    // GuardClauses: 'evaluates to true </returns>' became 'true. </returns>'.
    [Fact]
    public Task PeriodBeforeAClosingTag_TakesThePlaceOfTheSpaces() => VerifyFixAsync(
        """
        /// <summary>A guard{|BRO1603:|} </summary>
        public class Guard
        {
            /// <summary>Checks <see cref="Guard"/>{|BRO1603:|}  </summary>
            /// <param name="input">The input{|BRO1603:|}
            /// </param>
            /// <returns>Whatever the input evaluates to true{|BRO1603:|} </returns>
            public bool Check(bool input) => input;
        }
        """,
        """
        /// <summary>A guard.</summary>
        public class Guard
        {
            /// <summary>Checks <see cref="Guard"/>.</summary>
            /// <param name="input">The input.
            /// </param>
            /// <returns>Whatever the input evaluates to true.</returns>
            public bool Check(bool input) => input;
        }
        """);

    // Kavita: 'This is the Koreader hash' would become 'Gets or sets this is the Koreader hash'. Only summaries that start
    // like a noun phrase (an article or a lower-case word) or with a known prefix are reported (owner's decision,
    // 2026-10-09); a capitalized word has no fix that surely makes a sentence.
    [Fact]
    public Task PropertySummariesThatArentNounPhrases_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>This is the Koreader hash.</summary>
            public string Hash { get; set; }

            /// <summary>Not used - For parity.</summary>
            public string Document { get; set; }

            /// <summary>Device id of the user.</summary>
            public string DeviceId { get; set; }

            /// <summary>Is it closed.</summary>
            public bool IsClosed { get; set; }

            /// <summary>Theme of the page.</summary>
            public string Theme { get; set; }
        }
        """);

    // Dapper: 'If true, the command-text is inspected' became 'Gets or sets if true, ...', and a constructor summary
    // 'construct a dynamic parameter bag' became '... class. construct a dynamic parameter bag'. Text that can't follow
    // the standard words as a sentence is left alone.
    [Fact]
    public Task SummariesThatCantFollowTheStandardWords_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        /// <summary>Words.</summary>
        public class Words
        {
            /// <summary>construct a dynamic parameter bag.</summary>
            public Words()
            {
            }

            /// <summary>If set, the words are loaded.</summary>
            /// <param name="x">The x.</param>
            public Words(int x)
            {
            }

            /// <summary>When given, the words are copied.</summary>
            /// <param name="s">The s.</param>
            public Words(string s)
            {
            }

            // Scrutor: 'Initializes a new instance of the ... class. Initializes the attribute with ...' before.
            /// <summary>Initializes the words with the specified value.</summary>
            /// <param name="b">The b.</param>
            public Words(bool b)
            {
            }

            /// <summary>If true, the command-text is inspected.</summary>
            public bool Inspect { get; set; }

            /// <summary>True if it is open.</summary>
            public bool IsOpen { get; }

            /// <summary>Returns the name.</summary>
            public string Name { get; set; }

            /// <summary>whenever it changes, it's saved.</summary>
            public int Count { get; set; }

            /// <summary>returns the size.</summary>
            public int Size { get; set; }
        }
        """);
}
