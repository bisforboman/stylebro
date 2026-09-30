using StyleBro.Analyzers.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.CamelCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class CamelCaseNamingTests
{
    [Theory]
    [InlineData("Value", "value")]
    [InlineData("_value", "value")]
    [InlineData("__Value", "value")]
    [InlineData("URL", "url")]
    [InlineData("HTMLParser", "htmlParser")]
    [InlineData("IDs", "iDs")]
    [InlineData("A", "a")]
    [InlineData("Été", "été")]
    [InlineData("value", null)]
    [InlineData("vALUE", null)]
    [InlineData("_", null)]
    [InlineData("___", null)]
    [InlineData("_1", null)]
    [InlineData("Class", null)]
    public void NewName(string name, string? expected) => Assert.Equal(expected, CamelCaseNames.GetNewName(name));

    [Fact]
    public Task Variables_AreRenamedEverywhere() => VerifyFixAsync("""
        using System;
        using System.Linq;

        class C
        {
            int M()
            {
                int {|BRO1301:Total|} = 1;
                int {|BRO1301:_count|} = 2;
                var ({|BRO1301:A|}, b) = (1, 2);
                foreach (var {|BRO1301:Item|} in new[] { 1 })
                {
                    Total += Item;
                }

                try
                {
                }
                catch (Exception {|BRO1301:Ex|})
                {
                    Console.WriteLine(Ex.Message);
                }

                if (int.TryParse("1", out var {|BRO1301:Parsed|}) && Total is int {|BRO1301:Pattern|})
                {
                    Total += Parsed + Pattern;
                }

                using var {|BRO1301:Disposable|} = (IDisposable)null;
                var q = from {|BRO1301:Row|} in new[] { 1 } let {|BRO1301:Doubled|} = Row * 2 select Doubled;
                for (int {|BRO1301:I|} = 0; I < 1; I++)
                {
                }

                const int ConstLocal = 1;
                return Total + _count + A + b + ConstLocal + nameof(Total).Length;
            }
        }
        """, """
        using System;
        using System.Linq;

        class C
        {
            int M()
            {
                int total = 1;
                int count = 2;
                var (a, b) = (1, 2);
                foreach (var item in new[] { 1 })
                {
                    total += item;
                }

                try
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (int.TryParse("1", out var parsed) && total is int pattern)
                {
                    total += parsed + pattern;
                }

                using var disposable = (IDisposable)null;
                var q = from row in new[] { 1 } let doubled = row * 2 select doubled;
                for (int i = 0; i < 1; i++)
                {
                }

                const int ConstLocal = 1;
                return total + count + a + b + ConstLocal + nameof(total).Length;
            }
        }
        """);

    [Fact]
    public Task Parameters_AreRenamedWithNamedArgumentsAndDocs() => VerifyFixAsync("""
        using System;

        delegate void Handler(object {|BRO1302:Sender|});

        class Primary(int {|BRO1302:Count|})
        {
            public int Get() => Count;
        }

        class C
        {
            /// <summary>Uses <paramref name="Value"/>.</summary>
            /// <param name="Value">The value.</param>
            /// <param name="other">Another value.</param>
            public int M(int {|BRO1302:Value|}, int other)
            {
                if (Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Value));
                }

                Func<int, int> f = {|BRO1302:Arg|} => Arg;
                int Local(int {|BRO1302:Param|}) => Param;
                Action<int> h = delegate(int {|BRO1302:Anon|}) { };
                return f(Value) + Local(Param: other);
            }

            public int this[int {|BRO1302:Index|}] => Index;

            [System.Runtime.InteropServices.DllImport("x")]
            public static extern void Native(int {|BRO1302:Handle|});

            int N() => M(Value: 1, other: 2) + new Primary(Count: 3).Get();
        }
        """, """
        using System;

        delegate void Handler(object sender);

        class Primary(int count)
        {
            public int Get() => count;
        }

        class C
        {
            /// <summary>Uses <paramref name="value"/>.</summary>
            /// <param name="value">The value.</param>
            /// <param name="other">Another value.</param>
            public int M(int value, int other)
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                Func<int, int> f = arg => arg;
                int Local(int param) => param;
                Action<int> h = delegate(int anon) { };
                return f(value) + Local(param: other);
            }

            public int this[int index] => index;

            [System.Runtime.InteropServices.DllImport("x")]
            public static extern void Native(int handle);

            int N() => M(value: 1, other: 2) + new Primary(count: 3).Get();
        }
        """);

    [Fact]
    public Task BaseParameters_RenameOverridesAndImplementations() => VerifyFixAsync("""
        interface IThing
        {
            void Do(int {|BRO1302:Value|});
        }

        abstract class Base
        {
            public abstract void Run(int {|BRO1302:Speed|});
        }

        class Derived : Base, IThing
        {
            public override void Run(int Speed) => Do(Speed);

            public void Do(int Value)
            {
            }
        }

        class Explicit : IThing
        {
            void IThing.Do(int Value)
            {
            }
        }

        class Renamed : Base
        {
            public override void Run(int {|BRO1302:Pace|})
            {
            }
        }
        """, """
        interface IThing
        {
            void Do(int value);
        }

        abstract class Base
        {
            public abstract void Run(int speed);
        }

        class Derived : Base, IThing
        {
            public override void Run(int speed) => Do(speed);

            public void Do(int value)
            {
            }
        }

        class Explicit : IThing
        {
            void IThing.Do(int value)
            {
            }
        }

        class Renamed : Base
        {
            public override void Run(int pace)
            {
            }
        }
        """);

    [Fact]
    public Task NamedArgumentsInOtherFiles_AreRenamed() => VerifyFixAsync(
        [
            """
            class A
            {
                public static int M(int {|BRO1302:Value|}) => Value;
            }
            """,
            """
            class B
            {
                int N() => A.M(Value: 1);
            }
            """,
        ],
        [
            """
            class A
            {
                public static int M(int value) => value;
            }
            """,
            """
            class B
            {
                int N() => A.M(value: 1);
            }
            """,
        ]);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        record Person(string Name, int Age);

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit
            {
            }
        }

        interface IThing
        {
            void Do(int value);
        }

        class External : IComparable<External>
        {
            public int CompareTo(External other) => 0;
        }

        partial class P
        {
            partial void Q(int Value);

            partial void Q(int Value)
            {
            }
        }

        class C
        {
            int field;

            void M(int good, int _, int x1)
            {
                const int Constant = 1;
                int ___ = 2;
                int @class = 3;
                Func<int, int, int> g = (_, _) => 0;
            }
        }
        """);

    [Fact]
    public Task Conflicts_AreSkipped() => VerifyNoDiagnosticsAsync("""
        class C
        {
            int value;

            int Prop
            {
                get => value;
                set { var Value = value; }
            }

            int M(int Count)
            {
                var count = 1;
                return Count + count;
            }

            int N()
            {
                int Total = 1, _total = 2;
                return Total + _total;
            }

            object O()
            {
                int Size = 1;
                return new { Size };
            }

            (int, int) T()
            {
                int Width = 1;
                return (Width, 2);
            }

            int U(int Value)
            {
                return this.value + Value;
            }

            int V()
            {
                int Result = 1;
        #if !NEVER
                Result++;
        #endif
                return Result;
            }
        }
        """);

    [Fact]
    public Task OverrideConflict_KeepsThatOverridesName() => VerifyFixAsync("""
        abstract class Base
        {
            public abstract int Run(int {|BRO1302:Speed|});
        }

        class Derived : Base
        {
            int speed;

            public override int Run(int Speed) => Speed + speed;
        }
        """, """
        abstract class Base
        {
            public abstract int Run(int speed);
        }

        class Derived : Base
        {
            int speed;

            public override int Run(int Speed) => Speed + speed;
        }
        """);
}
