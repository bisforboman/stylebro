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
    [InlineData("Field", null)]
    [InlineData("_field", null)]
    [InlineData("RANGE_REP_REG", "rangeRepReg")]
    [InlineData("REP_REG", "repReg")]
    [InlineData("Max_Value", "maxValue")]
    [InlineData("_Foo_bar", "fooBar")]
    [InlineData("Int32_0", "int32_0")]
    [InlineData("CLASS_NAME", "className")]
    [InlineData("value_x", null)]
    public void NewName(string name, string? expected) => Assert.Equal(expected, CamelCaseNames.GetNewName(name));

    [Fact]
    public Task Variables_AreRenamedEverywhere() => VerifyFixAsync(
        """
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
                return Total + _count + A + b + ConstLocal + Total.ToString().Length;
            }
        }
        """,
        """
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
                return total + count + a + b + ConstLocal + total.ToString().Length;
            }
        }
        """);

    [Fact]
    public Task Parameters_AreRenamedWithNamedArgumentsAndDocs() => VerifyFixAsync(
        """
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
                    throw new ArgumentOutOfRangeException(Value.ToString());
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
        """,
        """
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
                    throw new ArgumentOutOfRangeException(value.ToString());
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
    public Task BaseParameters_RenameOverridesAndImplementations() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task OverrideConflict_KeepsThatOverridesName() => VerifyFixAsync(
        """
        abstract class Base
        {
            public abstract int Run(int {|BRO1302:Speed|});
        }

        class Derived : Base
        {
            int speed;

            public override int Run(int Speed) => Speed + speed;
        }
        """,
        """
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

    [Fact]
    public Task ConstructorParametersNamedLikeAMember_KeepTheirName() => VerifyFixAsync(
        """
        public class Point
        {
            public Point(int X, int {|BRO1302:Other|})
            {
                this.X = X;
                Y = Other;
            }

            public int X { get; }

            public int Y { get; }

            public void Move(int {|BRO1302:Y|}) { }
        }
        """,
        """
        public class Point
        {
            public Point(int X, int other)
            {
                this.X = X;
                Y = other;
            }

            public int X { get; }

            public int Y { get; }

            public void Move(int y) { }
        }
        """);

    [Fact]
    public Task NothingIsRenamedToField_ACSharp14KeywordInAccessors() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            int P
            {
                get
                {
                    var Field = 2;
                    return Field;
                }
            }

            void M(int Field) => System.Console.WriteLine(Field);
        }
        """);

    [Fact]
    public Task NothingIsRenamedToValue_InsideAPropertyIndexerOrEvent() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            int x;

            int P
            {
                get => x;
                set
                {
                    var Value = 2;
                    x = Value;
                }
            }

            int this[int Value]
            {
                get => Value;
                set => x = value;
            }
        }
        """);

    // Ocelot: a test read a private field with 'GetField(nameof(_components))', where '_components' was a local.
    [Fact]
    public Task NamesInNameof_KeepTheirName() => VerifyNoDiagnosticsAsync(
        """
        using System.Reflection;

        class C
        {
            object M(object builder)
            {
                object _components;
                _components = builder.GetType().GetField(nameof(_components), BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;
                return _components;
            }

            void N(string Value) => System.Console.WriteLine(nameof(Value));
        }
        """);

    [Fact]
    public Task PublicApiParameters_AreLeftAloneByDefault() => VerifyFixAsync(
        """
        interface IRun
        {
            void Run(int {|BRO1302:Count|});
        }

        public class Runner : IRun
        {
            public void Run(int Count) => System.Console.WriteLine(Count);
        }

        public class C
        {
            public void M(int Value) => System.Console.WriteLine(Value);

            protected void N(int Value) => System.Console.WriteLine(Value);

            internal void O(int {|BRO1302:Value|}) => System.Console.WriteLine(Value);

            private void P(int {|BRO1302:Value|}) => System.Console.WriteLine(Value);
        }
        """,
        """
        interface IRun
        {
            void Run(int count);
        }

        public class Runner : IRun
        {
            public void Run(int Count) => System.Console.WriteLine(Count);
        }

        public class C
        {
            public void M(int Value) => System.Console.WriteLine(Value);

            protected void N(int Value) => System.Console.WriteLine(Value);

            internal void O(int value) => System.Console.WriteLine(value);

            private void P(int value) => System.Console.WriteLine(value);
        }
        """,
        "stylebro_rename_public_api = false");

    // Bogus's Finance.cs: 'RANGE_REP_REG' became 'rangE_REP_REG' (only the leading capital run was lowered).
    [Fact]
    public Task UnderscoreSeparatedNames_AreJoinedWordByWord() => VerifyFixAsync(
        """
        internal class C
        {
            internal int M(int {|BRO1302:MAX_COUNT|})
            {
                var {|BRO1301:RANGE_REP_REG|} = "[0-9]";
                var {|BRO1301:REP_REG|} = RANGE_REP_REG + "+";
                return REP_REG.Length + MAX_COUNT;
            }
        }
        """,
        """
        internal class C
        {
            internal int M(int maxCount)
            {
                var rangeRepReg = "[0-9]";
                var repReg = rangeRepReg + "+";
                return repReg.Length + maxCount;
            }
        }
        """);
}
