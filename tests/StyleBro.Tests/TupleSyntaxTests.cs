using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.TupleSyntaxAnalyzer, StyleBro.CodeFixes.Readability.TupleSyntaxCodeFixProvider>;

namespace StyleBro.Tests;

public class TupleSyntaxTests
{
    [Fact]
    public Task Types_BecomeTupleTypes() => Verify.VerifyFixAsync("""
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
        """, """
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

    [Fact]
    public Task Creations_BecomeTupleLiterals() => Verify.VerifyFixAsync("""
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
        """, """
        using System;

        class C
        {
            void M()
            {
                var a = (1, "a");
                var b = (1, 2L);
                var c = ((long)1, (string)null);
                var d = (1 + 2, Count());
            }

            int Count() => 0;
        }
        """);

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
    public Task CreationWithANamingArgument_KeepsNewButConvertsInnerTypes() => Verify.VerifyFixAsync("""
        using System;

        class C
        {
            object M(int x) => new ValueTuple<{|BRO1123:ValueTuple<int, int>|}, int>(default, x);
        }
        """, """
        using System;

        class C
        {
            object M(int x) => new ValueTuple<(int, int), int>(default, x);
        }
        """);
}
