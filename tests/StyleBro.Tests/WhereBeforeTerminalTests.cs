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

    // Owner's decision (2026-10-10): where Sonar's S6605/S6602 are on, a List's or an array's own method, not the LINQ
    // call Sonar would report next (SonarAnalyzer.CSharp 9.19 on RealWorld: S6605 on 'Any(p)', S6602 on 'FirstOrDefault(p)').
    private const string Collections = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public void M(List<int> items, int[] numbers, Func<int, bool> test)
            {
                var any = items.{|BRO1150:Where|}(i => i > 0).Any();
                var first = items.{|BRO1150:Where|}(i => i > 0).FirstOrDefault();
                var count = items.{|BRO1150:Where|}(i => i > 0).Count();
                var anyArray = numbers.{|BRO1150:Where|}(i => i > 0).Any();
                var firstArray = numbers
                    .{|BRO1150:Where|}(i => i > 0)
                    .FirstOrDefault();
                var func = items.{|BRO1150:Where|}(test).Any();
            }
        }
        """;

    private const string Linq = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public void M(List<int> items, int[] numbers, Func<int, bool> test)
            {
                var any = items.Any(i => i > 0);
                var first = items.FirstOrDefault(i => i > 0);
                var count = items.Count(i => i > 0);
                var anyArray = numbers.Any(i => i > 0);
                var firstArray = numbers
                    .FirstOrDefault(i => i > 0);
                var func = items.Any(test);
            }
        }
        """;

    private const string Own = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public void M(List<int> items, int[] numbers, Func<int, bool> test)
            {
                var any = items.Exists(i => i > 0);
                var first = items.Find(i => i > 0);
                var count = items.Count(i => i > 0);
                var anyArray = Array.Exists(numbers, i => i > 0);
                var firstArray = Array.Find(numbers, i => i > 0);
                var func = items.Any(test);
            }
        }
        """;

    [Theory]
    [InlineData("dotnet_diagnostic.S6605.severity = warning\ndotnet_diagnostic.S6602.severity = warning", true)]
    [InlineData("build_property.StyleBroSonar = C:/nuget/sonaranalyzer.csharp/9.19.0.84025/analyzers/SonarAnalyzer.CSharp.dll", true)]
    [InlineData("build_property.StyleBroSonar = /home/x/.nuget/packages/sonaranalyzer.csharp/10.35.0.4138/analyzers/SonarAnalyzer.CSharp.dll", false)]
    [InlineData("build_property.StyleBroSonar = C:/nuget/sonaranalyzer.csharp/9.19.0.84025/analyzers/SonarAnalyzer.CSharp.dll\ndotnet_diagnostic.S6605.severity = none\ndotnet_diagnostic.S6602.severity = none", false)]
    [InlineData("build_property.StyleBroSonar = ", false)]
    public Task SonarCollectionRules_PickTheCollectionsOwnMethods(string editorConfig, bool own) =>
        Verify.VerifyFixAsync(Collections, own ? Own : Linq, editorConfig);

    [Fact]
    public Task ArraysWithoutUsingSystem_KeepTheLinqCall() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] numbers) => numbers.{|BRO1150:Where|}(i => i > 0).Any();
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] numbers) => numbers.Any(i => i > 0);
        }
        """,
        "dotnet_diagnostic.S6605.severity = warning");

    [Theory]
    [InlineData("C:/n/sonaranalyzer.csharp/9.19.0.84025/analyzers/SonarAnalyzer.CSharp.dll", 9)]
    [InlineData(@"C:\n\SonarAnalyzer.CSharp\10.3.0.106239\analyzers\SonarAnalyzer.CSharp.dll", 10)]
    [InlineData("C:/tools/SonarAnalyzer.CSharp.dll", null)]
    [InlineData("C:/a/other.dll|C:/n/sonaranalyzer.csharp/8.30.0.37606/analyzers/SonarAnalyzer.CSharp.dll", 8)]
    [InlineData("", null)]
    public void SonarVersion_IsReadFromThePackagePath(string paths, int? major) =>
        Assert.Equal(major, StyleBro.Analyzers.SonarRules.GetMajorVersion(paths));
    [Fact]
    public Task CollectionMethodsThatWouldNotBind_KeepTheLinqCall() => Verify.VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public static class Lists
        {
            public static bool Exists(this List<int> list, Func<int, bool> test) => true;
        }

        public class C
        {
            public bool M(List<int> items, int[] numbers, Func<int, bool> test) =>
                items.{|BRO1150:Where|}(test).Any() || numbers /* all */ .{|BRO1150:Where|}(i => i > 0).Any();
        }
        """,
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        public static class Lists
        {
            public static bool Exists(this List<int> list, Func<int, bool> test) => true;
        }

        public class C
        {
            public bool M(List<int> items, int[] numbers, Func<int, bool> test) =>
                items.Any(test) || numbers /* all */ .Any(i => i > 0);
        }
        """,
        "dotnet_diagnostic.S6605.severity = warning");
}
