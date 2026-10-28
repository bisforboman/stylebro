using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ConstantOnLeftAnalyzer, StyleBro.CodeFixes.Readability.ConstantOnLeftCodeFixProvider>;

namespace StyleBro.Tests;

public class ConstantOnLeftTests
{
    [Fact]
    public Task ConstantsOnTheLeft_AreSwapped() => VerifyFixAsync(
        """
        enum Color { Red, Green }

        class C
        {
            const int Max = 5;

            void M(int x, object o, string s, Color c, int? n)
            {
                bool b;
                b = {|BRO1103:1 == x|};
                b = {|BRO1103:null != o|};
                b = {|BRO1103:Max == x|};
                b = {|BRO1103:Color.Red == c|};
                b = {|BRO1103:"a" == s|};
                b = {|BRO1103:string.Empty == s|};
                b = {|BRO1103:1 == n|};
            }
        }
        """,
        """
        enum Color { Red, Green }

        class C
        {
            const int Max = 5;

            void M(int x, object o, string s, Color c, int? n)
            {
                bool b;
                b = x == 1;
                b = o != null;
                b = x == Max;
                b = c == Color.Red;
                b = s == "a";
                b = s == string.Empty;
                b = n == 1;
            }
        }
        """);

    [Fact]
    public Task RelationalOperators_AreFlipped() => VerifyFixAsync(
        """
        class C
        {
            void M(int x)
            {
                bool b;
                b = {|BRO1103:0 < x|};
                b = {|BRO1103:0 <= x|};
                b = {|BRO1103:0 > x|};
                b = {|BRO1103:0 >= x|};
            }
        }
        """,
        """
        class C
        {
            void M(int x)
            {
                bool b;
                b = x > 0;
                b = x >= 0;
                b = x < 0;
                b = x <= 0;
            }
        }
        """);

    [Fact]
    public Task NestedComparisons_AreFixedInOnePass() => VerifyFixAsync(
        """
        class C
        {
            bool M(int x) => {|BRO1103:true == ({|BRO1103:1 == x|})|};
        }
        """,
        """
        class C
        {
            bool M(int x) => (x == 1) == true;
        }
        """);

    [Fact]
    public Task SamePrecedenceOnTheLeft_GetsParentheses() => VerifyFixAsync(
        """
        class C
        {
            bool M(bool b) => {|BRO1103:1 == 2 == b|};
        }
        """,
        """
        class C
        {
            bool M(bool b) => b == (1 == 2);
        }
        """);

    [Fact]
    public Task MultiLineComparison_KeepsItsLayout() => VerifyFixAsync(
        """
        class C
        {
            bool M(int x) => {|BRO1103:1 ==
                x|};
        }
        """,
        """
        class C
        {
            bool M(int x) => x ==
                1;
        }
        """);

    [Fact]
    public Task CoreLibraryOperators_AreSwapped() => VerifyFixAsync(
        """
        using System;

        class C
        {
            bool M(Guid g, DateTime d) => {|BRO1103:Guid.Empty == g|} && {|BRO1103:DateTime.MinValue < d|};
        }
        """,
        """
        using System;

        class C
        {
            bool M(Guid g, DateTime d) => g == Guid.Empty && d > DateTime.MinValue;
        }
        """);

    [Fact]
    public Task UserDefinedOperator_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        class Money
        {
            public static bool operator ==(Money a, Money b) => true;
            public static bool operator !=(Money a, Money b) => false;
            public override bool Equals(object o) => true;
            public override int GetHashCode() => 0;
        }

        class C
        {
            bool M(Money m) => null == m;
        }
        """);

    [Fact]
    public Task BothOrNeitherConstant_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            bool M(int x, int y) => x == y || 1 == 2 || x == 1;
        }
        """);

    [Fact]
    public Task ExpressionTrees_KeepTheirOperands() => VerifyFixAsync(
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        class C
        {
            // Trial 58 (ExpressionTreeToString): swapping changes the tree ('<=' becomes '>='; owner, 2026-10-27).
            Expression<Func<DayOfWeek, bool>> e = d => DayOfWeek.Tuesday <= d;
            Func<DayOfWeek, bool> f = d => {|BRO1103:DayOfWeek.Tuesday <= d|};

            IQueryable<string> Q(IQueryable<string> q) => from s in q where 'c' == s[0] select s;
        }
        """,
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        class C
        {
            // Trial 58 (ExpressionTreeToString): swapping changes the tree ('<=' becomes '>='; owner, 2026-10-27).
            Expression<Func<DayOfWeek, bool>> e = d => DayOfWeek.Tuesday <= d;
            Func<DayOfWeek, bool> f = d => d >= DayOfWeek.Tuesday;

            IQueryable<string> Q(IQueryable<string> q) => from s in q where 'c' == s[0] select s;
        }
        """);

    [Fact]
    public Task ExpressionTreeTwins_KeepTheirOperands() => VerifyFixAsync(
        """
        using System;
        using System.Linq.Expressions;

        enum E { Foo, Bar }

        class C
        {
            // Trial 59 (DelegateDecompiler): the delegate is decompiled and compared with its twin tree (owner, 2026-10-28).
            void M()
            {
                Expression<Func<E, bool>> expected = x => E.Bar == x;
                Func<E, bool> compiled = x => E.Bar == x;
                Func<E, bool> other = x => {|BRO1103:E.Foo == x|};
            }
        }
        """,
        """
        using System;
        using System.Linq.Expressions;

        enum E { Foo, Bar }

        class C
        {
            // Trial 59 (DelegateDecompiler): the delegate is decompiled and compared with its twin tree (owner, 2026-10-28).
            void M()
            {
                Expression<Func<E, bool>> expected = x => E.Bar == x;
                Func<E, bool> compiled = x => E.Bar == x;
                Func<E, bool> other = x => x == E.Foo;
            }
        }
        """);
}
