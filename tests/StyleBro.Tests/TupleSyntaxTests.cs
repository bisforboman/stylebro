using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.TupleSyntaxAnalyzer, StyleBro.CodeFixes.Readability.TupleSyntaxCodeFixProvider>;

namespace StyleBro.Tests;

public class TupleSyntaxTests
{
    [Fact]
    public Task Types_BecomeTupleTypes() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;

        class C
        {
            {|BRO1123:ValueTuple<int, string>|} field;

            {|BRO1123:System.ValueTuple<int, ValueTuple<int, int>>|} Nested(List<{|BRO1123:ValueTuple<int, int>|}> items) => default;

            {|BRO1123:ValueTuple<int, int>|}[] array;

            {|BRO1123:ValueTuple<int, int>|}? maybe;

            void M()
            {
                {|BRO1123:ValueTuple<long, long>|} local = default({|BRO1123:ValueTuple<long, long>|});
                var type = typeof({|BRO1123:ValueTuple<int, int>|});
                var empty = new List<{|BRO1123:ValueTuple<int, int>|}>();
            }
        }
        """,
        """
        using System;
        using System.Collections.Generic;

        class C
        {
            (int, string) field;

            (int, (int, int)) Nested(List<(int, int)> items) => default;

            (int, int)[] array;

            (int, int)? maybe;

            void M()
            {
                (long, long) local = default((long, long));
                var type = typeof((int, int));
                var empty = new List<(int, int)>();
            }
        }
        """);

    // akka.net: '(T, T)' after 'is' or 'case' is a positional pattern (deconstruction), not a type test: CS8129, CS1061.
    [Fact]
    public Task PatternTypes_StayValueTuple() => Verify.VerifyFixAsync(
        """
        using System;

        class C<T>
        {
            int M(object message)
            {
                if (message is ValueTuple<T, T> pair)
                {
                    return 1;
                }

                if (message is ValueTuple<int, string> || message is ValueTuple<int, int> { Item1: 1 })
                {
                    return 2;
                }

                switch (message)
                {
                    case ValueTuple<string, int> props:
                        return props.Item2;
                    case ValueTuple<long, long>:
                        return 3;
                }

                return message switch
                {
                    ValueTuple<{|BRO1123:ValueTuple<int, int>|}, int> nested => nested.Item2,
                    _ => 0,
                };
            }
        }
        """,
        """
        using System;

        class C<T>
        {
            int M(object message)
            {
                if (message is ValueTuple<T, T> pair)
                {
                    return 1;
                }

                if (message is ValueTuple<int, string> || message is ValueTuple<int, int> { Item1: 1 })
                {
                    return 2;
                }

                switch (message)
                {
                    case ValueTuple<string, int> props:
                        return props.Item2;
                    case ValueTuple<long, long>:
                        return 3;
                }

                return message switch
                {
                    ValueTuple<(int, int), int> nested => nested.Item2,
                    _ => 0,
                };
            }
        }
        """);

    [Fact]
    public Task Creations_BecomeTupleLiterals() => Verify.VerifyFixAsync(
        """
        using System;

        class C
        {
            void M()
            {
                var a = {|BRO1123:new ValueTuple<int, string>(1, "a")|};
                var b = {|BRO1123:ValueTuple.Create|}(1, 2L);
                var c = {|BRO1123:new ValueTuple<long, string>(1, null)|};
                var d = {|BRO1123:new ValueTuple<int, int>(1 + 2, Count())|};
            }

            int Count() => 0;
        }
        """,
        """
        using System;

        class C
        {
            void M()
            {
                var a = (1, "a");
                var b = (1, 2L);
                var c = (1L, (string)null);
                var d = (1 + 2, Count());
            }

            int Count() => 0;
        }
        """);

    [Fact]
    public Task ConvertedLiterals_GetBRO1122sSuffix_OnlyWhereItIsOn()
    {
        // Ursa.Avalonia: 'new ValueTuple<double, double, double>(0, 0, 0)' became '((double)0, ...)', which BRO1122 turned into
        // '0D' in a second run. A suffix only where its value is exact: (float)0.1 isn't 0.1F.
        const string Source = """
            using System;

            class C
            {
                void M()
                {
                    var a = {|BRO1123:new ValueTuple<double, double, float>(0, 1, 2)|};
                    var b = {|BRO1123:new ValueTuple<ulong, decimal>(1, 2)|};
                }
            }
            """;
        return Task.WhenAll(
            Verify.VerifyFixAsync(Source, Source.Replace("{|BRO1123:new ValueTuple<double, double, float>(0, 1, 2)|}", "(0D, 1D, 2F)").Replace("{|BRO1123:new ValueTuple<ulong, decimal>(1, 2)|}", "(1UL, 2M)")),
            Verify.VerifyFixAsync(Source, Source.Replace("{|BRO1123:new ValueTuple<double, double, float>(0, 1, 2)|}", "((double)0, (double)1, (float)2)").Replace("{|BRO1123:new ValueTuple<ulong, decimal>(1, 2)|}", "((ulong)1, (decimal)2)"), editorConfig: "dotnet_diagnostic.BRO1122.severity = none"),
            Verify.VerifyFixAsync(Source, Source.Replace("{|BRO1123:new ValueTuple<double, double, float>(0, 1, 2)|}", "(0D, 1D, 2F)").Replace("{|BRO1123:new ValueTuple<ulong, decimal>(1, 2)|}", "(1ul, 2M)"), editorConfig: "dotnet_diagnostic.BRO1135.severity = none"));
    }

    [Fact]
    public Task CreationsThatWouldChangeOrNotCompile_AreNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        using System;
        using System.Linq.Expressions;

        class C
        {
            void M(int x, (int A, int B) named)
            {
                // Arguments that would name the elements.
                var a = ValueTuple.Create(x, 1);
                var b = new ValueTuple<int, int>(named.A, 1);
                // No arguments, one element, an initializer, named arguments.
                var c = new ValueTuple<int, int>();
                var d = ValueTuple.Create(1);
                var e = new ValueTuple<int, int>(1, 2) { Item1 = 3 };
                var f = new ValueTuple<int, int>(item2: 2, item1: 1);
                // Expression trees can't contain tuple literals.
                Expression<Func<(int, int)>> g = () => ValueTuple.Create(1, 2);
                // Not a tuple type, and an open generic.
                var h = typeof(ValueTuple<>);
                var i = typeof(ValueTuple<,>);
            }
        }
        """);

    [Fact]
    public Task CreationWithANamingArgument_KeepsNewButConvertsInnerTypes() => Verify.VerifyFixAsync(
        """
        using System;

        class C
        {
            object M(int x) => new ValueTuple<{|BRO1123:ValueTuple<int, int>|}, int>(default, x);
        }
        """,
        """
        using System;

        class C
        {
            object M(int x) => new ValueTuple<(int, int), int>(default, x);
        }
        """);
}
