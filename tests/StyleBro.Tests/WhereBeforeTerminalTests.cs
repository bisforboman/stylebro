using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.WhereBeforeTerminalAnalyzer, StyleBro.CodeFixes.Readability.WhereBeforeTerminalCodeFixProvider>;

namespace StyleBro.Tests;

public class WhereBeforeTerminalTests
{
    [Fact]
    public Task WhereThenTerminal_TakesThePredicate() => Verify.VerifyFixAsync(
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public void M(List<int> items, IQueryable<int> query)
            {
                var any = items.{|BRO1150:Where|}(i => i > 0).Any();
                var count = items.{|BRO1150:Where|}(i => i > 0).Count();
                var longCount = items.{|BRO1150:Where|}(i => i > 0).LongCount();
                var first = items.{|BRO1150:Where|}(i => i > 0).First();
                var firstOrDefault = items.{|BRO1150:Where|}(i => i > 0).FirstOrDefault();
                var last = items.{|BRO1150:Where|}(i => i > 0).Last();
                var lastOrDefault = items.{|BRO1150:Where|}(i => i > 0).LastOrDefault();
                var single = items.{|BRO1150:Where|}(i => i > 0).Single();
                var singleOrDefault = items.{|BRO1150:Where|}(i => i > 0).SingleOrDefault();
                var queried = query.{|BRO1150:Where|}(i => i > 0).Count();
                var nested = items.{|BRO1150:Where|}(i => items.{|BRO1150:Where|}(j => j > i).Any()).Count();
            }
        }
        """,
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public void M(List<int> items, IQueryable<int> query)
            {
                var any = items.Any(i => i > 0);
                var count = items.Count(i => i > 0);
                var longCount = items.LongCount(i => i > 0);
                var first = items.First(i => i > 0);
                var firstOrDefault = items.FirstOrDefault(i => i > 0);
                var last = items.Last(i => i > 0);
                var lastOrDefault = items.LastOrDefault(i => i > 0);
                var single = items.Single(i => i > 0);
                var singleOrDefault = items.SingleOrDefault(i => i > 0);
                var queried = query.Count(i => i > 0);
                var nested = items.Count(i => items.Any(j => j > i));
            }
        }
        """);

    [Fact]
    public Task SplitChains_KeepTheirLines() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] items) =>
                items
                    .Select(i => i * 2)
                    .{|BRO1150:Where|}(i => i > 0)
                    .Any();
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] items) =>
                items
                    .Select(i => i * 2)
                    .Any(i => i > 0);
        }
        """);

    [Fact]
    public Task OtherCalls_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;

        public class Bag
        {
            public Bag Where(Func<int, bool> predicate) => this;

            public int Count() => 0;
        }

        public class Shelf : List<int>
        {
            public IEnumerable<int> Where(Func<int, bool> predicate) => new[] { 1 };
        }

        public class Items : List<int>
        {
            public bool Any(Func<int, bool> predicate) => false;
        }

        public class C
        {
            public void M(List<int> items, Bag bag, Items mine, Shelf shelf)
            {
                var indexed = items.Where((i, index) => i > index).Count();
                var other = items.Where(i => i > 0).ToList();
                var own = bag.Where(i => i > 0).Count();
                var instance = mine.Where(i => i > 0).Any();
                var shelved = shelf.Where(i => i > 0).Count();
                var conditional = items?.Where(i => i > 0).Count();
                var commented = items.Where(i => i > 0) /* positives */ .Count();
                Expression<Func<List<int>, int>> tree = list => list.Where(i => i > 0).Count();
            }
        }
        """);
}
