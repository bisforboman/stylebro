using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.AccessModifiersAnalyzer, StyleBro.CodeFixes.Maintainability.AccessModifiersCodeFixProvider>;

namespace StyleBro.Tests;

public class AccessModifiersTests
{
    [Fact]
    public Task MissingModifiers_GetTheDefault() => VerifyFixAsync(
        """
        using System;

        class {|BRO1404:Outer|}
        {
            int {|BRO1404:count|}, other;

            static readonly int {|BRO1404:Max|} = 1;

            event EventHandler {|BRO1404:Changed|};

            [Obsolete]
            {|BRO1404:Outer|}()
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }

            static Outer()
            {
            }

            int {|BRO1404:Size|} { get; set; }

            int {|BRO1404:this|}[int i] => i + count + other + Max;

            void {|BRO1404:Run|}()
            {
            }

            class {|BRO1404:Nested|}
            {
            }

            delegate void {|BRO1404:Handler|}();
        }

        struct {|BRO1404:Point|}
        {
        }

        enum {|BRO1404:Kind|}
        {
            A,
        }

        interface {|BRO1404:IShape|}
        {
            void Draw();
        }
        """,
        """
        using System;

        internal class Outer
        {
            private int count, other;

            private static readonly int Max = 1;

            private event EventHandler Changed;

            [Obsolete]
            private Outer()
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }

            static Outer()
            {
            }

            private int Size { get; set; }

            private int this[int i] => i + count + other + Max;

            private void Run()
            {
            }

            private class Nested
            {
            }

            private delegate void Handler();
        }

        internal struct Point
        {
        }

        internal enum Kind
        {
            A,
        }

        internal interface IShape
        {
            void Draw();
        }
        """);

    [Fact]
    public Task PartialTypes_GetTheOtherPartsModifier() => VerifyFixAsync(
        """
        public partial class Widget
        {
        }

        partial class {|BRO1007:Widget|}
        {
            partial void OnCreated();

            partial class {|BRO1007:Part|}
            {
            }
        }

        static partial class {|BRO1007:Helpers|}
        {
        }
        """,
        """
        public partial class Widget
        {
        }

        public partial class Widget
        {
            partial void OnCreated();

            private partial class Part
            {
            }
        }

        internal static partial class Helpers
        {
        }
        """);

    [Fact]
    public Task Always_AlsoAsksForInterfaceMembers() => VerifyFixAsync(
        """
        public interface IShape
        {
            double {|BRO1404:Area|}();

            string {|BRO1404:Name|} { get; }
        }
        """,
        """
        public interface IShape
        {
            public double Area();

            public string Name { get; }
        }
        """,
        editorConfig: "dotnet_style_require_accessibility_modifiers = always:warning");

    [Theory]
    [InlineData("never")]
    [InlineData("omit_if_default")]
    public Task NeverAndOmitIfDefault_AskForNothing(string value) => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            int count;

            void M() => count++;
        }

        partial class P
        {
        }
        """,
        "dotnet_style_require_accessibility_modifiers = " + value);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        public interface IShape
        {
            void Draw();

            class Inner
            {
            }
        }

        public class Shape : IShape, IDisposable
        {
            void IShape.Draw()
            {
            }

            void IDisposable.Dispose()
            {
            }

            public static Shape operator +(Shape a, Shape b) => a;

            ~Shape()
            {
            }
        }

        file class Local
        {
        }

        file partial class FileLocal
        {
        }

        partial class FileLocal
        {
        }
        """);
}
