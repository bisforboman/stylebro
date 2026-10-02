using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.LambdaSyntaxAnalyzer, StyleBro.CodeFixes.Readability.LambdaSyntaxCodeFixProvider>;

namespace StyleBro.Tests;

public class LambdaSyntaxTests
{
    [Fact]
    public Task AnonymousMethods_BecomeLambdas() => Verify.VerifyFixAsync(
        """
        using System;

        class C
        {
            event EventHandler Changed;

            Func<int, int> twice = {|BRO1125:delegate|} (int x) { return x * 2; };

            void M()
            {
                Action a = {|BRO1125:delegate|} { };
                Action b = {|BRO1125:delegate|}() { };
                Func<int, int, int> add = {|BRO1125:delegate|} (int x, int y) { return x + y; };
                Changed += {|BRO1125:delegate|} { Console.WriteLine(); };
                var c = (Action){|BRO1125:delegate|} { };
                Func<System.Threading.Tasks.Task> d = async {|BRO1125:delegate|} { await System.Threading.Tasks.Task.Yield(); };
                Changed += {|BRO1125:delegate|} (object s, EventArgs e)
                {
                    Console.WriteLine(s);
                };
            }
        }
        """,
        """
        using System;

        class C
        {
            event EventHandler Changed;

            Func<int, int> twice = x => { return x * 2; };

            void M()
            {
                Action a = () => { };
                Action b = () => { };
                Func<int, int, int> add = (x, y) => { return x + y; };
                Changed += (sender, e) => { Console.WriteLine(); };
                var c = (Action)(() => { });
                Func<System.Threading.Tasks.Task> d = async () => { await System.Threading.Tasks.Task.Yield(); };
                Changed += (s, e) =>
                {
                    Console.WriteLine(s);
                };
            }
        }
        """);

    [Fact]
    public Task GeneratedNames_AvoidNamesInScopeAndInTheBody() => Verify.VerifyFixAsync(
        """
        using System;

        class C
        {
            event EventHandler Changed;

            void M(object sender)
            {
                Changed += {|BRO1125:delegate|} { var e = 1; Console.WriteLine(e); };
            }
        }
        """,
        """
        using System;

        class C
        {
            event EventHandler Changed;

            void M(object sender)
            {
                Changed += (sender1, e1) => { var e = 1; Console.WriteLine(e); };
            }
        }
        """);

    [Fact]
    public Task OverloadThatWouldChange_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        using System;
        using System.Linq.Expressions;

        class C
        {
            void Run(Func<int> f) { }

            void Run(Expression<Func<int>> f) { }

            void Use(Action<int> a) { }

            void Use(Action<string> a) { }

            void M()
            {
                // With a lambda, the Expression overload would win.
                Run(delegate { return 1; });
                // Without the parameter type, the call is ambiguous.
                Use(delegate (int x) { });
            }
        }
        """);

    [Fact]
    public Task Arguments_BecomeLambdasWhenTheSameOverloadIsCalled() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;

        class C
        {
            void Use(Action<int> a) { }

            void M(List<int> items)
            {
                Use({|BRO1125:delegate|} (int x) { });
                items.ForEach({|BRO1125:delegate|} (int item) { Console.WriteLine(item); });
                items.RemoveAll(match: {|BRO1125:delegate|} (int i) { return i > 1; });
            }
        }
        """,
        """
        using System;
        using System.Collections.Generic;

        class C
        {
            void Use(Action<int> a) { }

            void M(List<int> items)
            {
                Use(x => { });
                items.ForEach(item => { Console.WriteLine(item); });
                items.RemoveAll(match: i => { return i > 1; });
            }
        }
        """);

    [Fact]
    public Task RefParametersAndNaturalTypes_AreNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        delegate void Swap(ref int a, ref int b);

        class C
        {
            void M()
            {
                Swap s = delegate { };
                Swap t = delegate (ref int a, ref int b) { };
                var u = delegate (int x) { return x; };
            }
        }
        """);

    [Fact]
    public Task EmptyParentheses_AreRemovedWhenLambdasAreOff() => Verify.VerifyFixAsync(
        """
        using System;

        class C
        {
            void M()
            {
                Action a = delegate{|BRO1403:()|} { };
                Action b = delegate {|BRO1403:()|}{ };
            }
        }
        """,
        """
        using System;

        class C
        {
            void M()
            {
                Action a = delegate { };
                Action b = delegate { };
            }
        }
        """,
        "dotnet_diagnostic.BRO1125.severity = none\n");

    [Fact]
    public Task EmptyParenthesesNeededForAnOverload_AreKept() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;

        class C
        {
            void Run(Action a) { }

            void Run(Action<int> a) { }

            void M() => Run(delegate() { });
        }
        """,
        "dotnet_diagnostic.BRO1125.severity = none\n");
}
