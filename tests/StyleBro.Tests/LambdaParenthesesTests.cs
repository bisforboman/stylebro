using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.LambdaParenthesesAnalyzer, StyleBro.CodeFixes.Readability.LambdaParenthesesCodeFixProvider>;

namespace StyleBro.Tests;

public class LambdaParenthesesTests
{
    [Fact]
    public Task SingleUntypedParameter_LosesItsParentheses() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Threading.Tasks;

        public class C
        {
            public Func<int, int> A = {|BRO1136:(x)|} => x;
            public Func<int, Func<int, int>> B = {|BRO1136:( x )|} => {|BRO1136:(y)|} => x + y;
            public Func<int, Task> D = async {|BRO1136:(x)|} => await Task.Delay(x);
            public Func<int, int> E = static {|BRO1136:(_)|} => 1;
        }
        """,
        """
        using System;
        using System.Threading.Tasks;

        public class C
        {
            public Func<int, int> A = x => x;
            public Func<int, Func<int, int>> B = x => y => x + y;
            public Func<int, Task> D = async x => await Task.Delay(x);
            public Func<int, int> E = static _ => 1;
        }
        """);

    [Fact]
    public Task ParenthesesThatAreNeeded_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;

        public delegate void R(ref int x);

        public class A : Attribute
        {
        }

        public class C
        {
            public Func<int> None = () => 1;
            public Func<int, int, int> Two = (x, y) => x + y;
            public Func<int, int> Typed = (int x) => x;
            public R Ref = (ref int x) => x++;
            public Func<int, int> Attributed = ([A] x) => x;
            public Func<int, int> OnLambda = [A] (x) => x;
            public Func<int, int> Returns = int (x) => x;
            public Func<int, int> Comment = (/* value */ x) => x;
        }
        """);
}
