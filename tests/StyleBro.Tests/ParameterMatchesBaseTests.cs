using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.CamelCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class ParameterMatchesBaseTests
{
    private const string On = "dotnet_diagnostic.BRO1313.severity = warning";

    [Fact]
    public Task OverridesAndImplementations_TakeTheBaseName_WithNamedArguments() => VerifyFixAsync(
        """
        public interface IStore
        {
            void Save(int id, string name);

            int this[int index] { get; }
        }

        public class Store : IStore
        {
            public void Save(int {|BRO1313:key|}, string name) => System.Console.WriteLine(key);

            public int this[int {|BRO1313:i|}] => i;
        }

        public class Explicit : IStore
        {
            void IStore.Save(int {|BRO1313:number|}, string {|BRO1313:text|}) => System.Console.WriteLine(number + text);

            int IStore.this[int index] => index;
        }

        public class Shape
        {
            public override bool Equals(object {|BRO1313:other|}) => other is Shape;

            public override int GetHashCode() => 0;

            public void Use(Store store) => store.Save(key: 1, name: "a");
        }
        """,
        """
        public interface IStore
        {
            void Save(int id, string name);

            int this[int index] { get; }
        }

        public class Store : IStore
        {
            public void Save(int id, string name) => System.Console.WriteLine(id);

            public int this[int index] => index;
        }

        public class Explicit : IStore
        {
            void IStore.Save(int id, string name) => System.Console.WriteLine(id + name);

            int IStore.this[int index] => index;
        }

        public class Shape
        {
            public override bool Equals(object obj) => obj is Shape;

            public override int GetHashCode() => 0;

            public void Use(Store store) => store.Save(id: 1, name: "a");
        }
        """,
        On);

    [Fact]
    public Task ChainOfOverrides_ConvergesInOneRun() => VerifyFixAsync(
        """
        public abstract class A
        {
            public abstract void Run(int {|BRO1302:Count|});
        }

        public class B : A
        {
            public override void Run(int {|BRO1313:n|})
            {
            }
        }

        public class C : B
        {
            public override void Run(int {|BRO1313:m|})
            {
            }
        }
        """,
        """
        public abstract class A
        {
            public abstract void Run(int count);
        }

        public class B : A
        {
            public override void Run(int count)
            {
            }
        }

        public class C : B
        {
            public override void Run(int count)
            {
            }
        }
        """,
        On);

    [Fact]
    public Task OffByDefault_AndBaseNamesAreKept() => VerifyNoDiagnosticsAsync(
        """
        public class Shape
        {
            public override bool Equals(object other) => other is Shape;

            public override int GetHashCode() => 0;
        }
        """);

    [Fact]
    public Task SkippedParameters_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        public interface IFirst
        {
            void Run(int first);
        }

        public interface ISecond
        {
            void Run(int second);
        }

        public class Both : IFirst, ISecond
        {
            public void Run(int value)
            {
            }
        }

        public class Shape
        {
            public override bool Equals(object _) => false;

            public override int GetHashCode() => 0;
        }

        public class Clash
        {
            public override bool Equals(object other)
            {
                var obj = other;
                return obj is Clash;
            }

            public override int GetHashCode() => 0;
        }

        public partial class Part : IFirst
        {
            public partial void Run(int value);
        }

        public partial class Part
        {
            public partial void Run(int value)
            {
            }
        }

        public class Plain
        {
            public void Run(int value)
            {
            }
        }

        public class Keyword
        {
            public virtual void Run(int @class)
            {
            }
        }

        public class FromKeyword : Keyword
        {
            public override void Run(int value)
            {
            }
        }
        """,
        On);

    [Fact]
    public Task BaseWhoseOwnRenameIsBlocked_CountsWithItsCurrentName() => VerifyFixAsync(
        """
        public class A
        {
            public virtual int Run(int Count)
            {
                var count = Count;
                return count;
            }
        }

        public class B : A
        {
            public override int Run(int {|BRO1313:n|}) => n;
        }
        """,
        """
        public class A
        {
            public virtual int Run(int Count)
            {
                var count = Count;
                return count;
            }
        }

        public class B : A
        {
            public override int Run(int Count) => Count;
        }
        """,
        On);

    [Fact]
    public Task WithHungarianOn_TheBaseNameWins() => VerifyFixAsync(
        """
        public interface IStore
        {
            void Save(int count);
        }

        public class Store : IStore
        {
            public void Save(int {|BRO1313:iCount|})
            {
            }
        }
        """,
        """
        public interface IStore
        {
            void Save(int count);
        }

        public class Store : IStore
        {
            public void Save(int count)
            {
            }
        }
        """,
        On + "\ndotnet_diagnostic.BRO1310.severity = warning");

    // Newtonsoft.Json: JObject.ContainsKey(propertyName) throws ArgumentNullException(nameof(propertyName)), and a test
    // expects that ParamName. An override renamed along with its base keeps such a name too.
    [Fact]
    public Task NamesThatReachRunTime_AreKept() => VerifyFixAsync(
        """
        using System;
        using System.Runtime.CompilerServices;

        public interface IStore
        {
            bool Has(string key);

            void Run(int id);

            void Check(string value);
        }

        public class Store : IStore
        {
            public bool Has(string propertyName) => propertyName != null ? true : throw new ArgumentNullException(nameof(propertyName));

            public virtual void Run(int {|BRO1313:number|}) => Console.WriteLine(number);

            public void Check(string text) => Guard.NotNull(text);
        }

        public class Derived : Store
        {
            public override void Run(int number) => throw new ArgumentOutOfRangeException(nameof(number));
        }

        public static class Guard
        {
            public static void NotNull(object argument, [CallerArgumentExpression("argument")] string name = "") => Console.WriteLine(name);
        }
        """,
        """
        using System;
        using System.Runtime.CompilerServices;

        public interface IStore
        {
            bool Has(string key);

            void Run(int id);

            void Check(string value);
        }

        public class Store : IStore
        {
            public bool Has(string propertyName) => propertyName != null ? true : throw new ArgumentNullException(nameof(propertyName));

            public virtual void Run(int id) => Console.WriteLine(id);

            public void Check(string text) => Guard.NotNull(text);
        }

        public class Derived : Store
        {
            public override void Run(int number) => throw new ArgumentOutOfRangeException(nameof(number));
        }

        public static class Guard
        {
            public static void NotNull(object argument, [CallerArgumentExpression("argument")] string name = "") => Console.WriteLine(name);
        }
        """,
        On);
}
