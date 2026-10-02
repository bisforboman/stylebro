using StyleBro.Analyzers.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.PascalCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class PascalCaseNamingTests
{
    [Theory]
    [InlineData("lowerMethod", "LowerMethod")]
    [InlineData("_helper", "Helper")]
    [InlineData("Upper", null)]
    [InlineData("_Upper", "Upper")]
    [InlineData("_", null)]
    [InlineData("_1", null)]
    public void NewName(string name, string? expected) => Assert.Equal(expected, PascalCaseNamingAnalyzer.GetNewName(name));

    [Fact]
    public Task TypesAndMembers_AreRenamedEverywhere() => VerifyFixAsync(
        """
        using System;

        public class {|BRO1309:shape|}
        {
            public shape()
            {
            }

            public shape(int sides) : this()
            {
                changed?.Invoke(this, EventArgs.Empty);
            }

            ~shape()
            {
            }

            public int {|BRO1309:sides|} { get; set; }

            public event EventHandler {|BRO1309:changed|};

            public enum {|BRO1309:kind|}
            {
                {|BRO1309:round|},
                Square,
            }

            public delegate void {|BRO1309:handler|}();

            public int {|BRO1309:area|}() => helper() * sides;

            private int {|BRO1309:helper|}()
            {
                int {|BRO1309:local|}() => 2;
                return local();
            }

            public static shape Create() => new shape(3) { sides = 4 };

            public kind Current => kind.round;

            public string Name => nameof(area);
        }
        """,
        """
        using System;

        public class Shape
        {
            public Shape()
            {
            }

            public Shape(int sides) : this()
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }

            ~Shape()
            {
            }

            public int Sides { get; set; }

            public event EventHandler Changed;

            public enum Kind
            {
                Round,
                Square,
            }

            public delegate void Handler();

            public int Area() => Helper() * Sides;

            private int Helper()
            {
                int Local() => 2;
                return Local();
            }

            public static Shape Create() => new Shape(3) { Sides = 4 };

            public Kind Current => Kind.Round;

            public string Name => nameof(Area);
        }
        """.Replace("public enum Kind", "public enum Kind").Replace("public Kind Current => Kind.Round;", "public Kind Current => Kind.Round;"));

    [Fact]
    public Task BaseMembers_RenameOverridesAndImplementations() => VerifyFixAsync(
        """
        public interface IShape
        {
            double {|BRO1309:area|}();

            string {|BRO1309:name|} { get; }
        }

        public abstract class Base
        {
            public abstract void {|BRO1309:draw|}();
        }

        public class Circle : Base, IShape
        {
            public double area() => 1;

            string IShape.name => "circle";

            public override void draw()
            {
            }

            public double Twice() => area() * 2;
        }

        public class Use
        {
            public double Get(IShape s, Base b)
            {
                b.draw();
                return s.area() + s.name.Length;
            }
        }
        """,
        """
        public interface IShape
        {
            double Area();

            string Name { get; }
        }

        public abstract class Base
        {
            public abstract void Draw();
        }

        public class Circle : Base, IShape
        {
            public double Area() => 1;

            string IShape.Name => "circle";

            public override void Draw()
            {
            }

            public double Twice() => Area() * 2;
        }

        public class Use
        {
            public double Get(IShape s, Base b)
            {
                b.Draw();
                return s.Area() + s.Name.Length;
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;
        using System.Runtime.InteropServices;

        public interface iThing
        {
        }

        public record Person(string name);

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
        }

        public class External : IComparable
        {
            public int CompareTo(object obj) => 0;

            public override string ToString() => string.Empty;
        }

        public class C
        {
            private int field;

            public int Value;

            [DllImport("x")]
            private static extern int native();

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
            private static extern int internalCall();

            [Obsolete]
            public int attributed { get; set; }

            public int this[int i] => i;

            public static C operator +(C a, C b) => a;

            partial class P
            {
                partial void q();

                partial void q()
                {
                }
            }
        }

        public class Paired
        {
            public int items { get; set; }

            public bool ShouldSerializeitems() => items > 0;
        }

        public class JsonObjectAttribute : Attribute
        {
        }

        [JsonObject]
        public class Serialized
        {
            public int value { get; set; }
        }

        public partial class Conflict
        {
            public void run()
            {
            }

            public void Run(int x)
            {
            }
        }
        """);

    [Fact]
    public Task DerivedMemberWithTheNewName_KeepsTheName() => VerifyNotFixedAsync(
        [
            """
            public class Base
            {
                public virtual int {|BRO1309:size|}() => 1;
            }
            """,
            """
            public class Derived : Base
            {
                public override int size() => 2;

                public int Size => 3;
            }
            """,
        ]);

    [Fact]
    public Task PropertyNamesInStrings_KeepTheName() => VerifyNotFixedAsync(
        [
            """
            public class Dto
            {
                public string {|BRO1309:access_token|} { get; set; }
            }
            """,
            """
            class Tests
            {
                const string Json = "{\"access_token\": \"x\"}";
            }
            """,
        ]);
}
