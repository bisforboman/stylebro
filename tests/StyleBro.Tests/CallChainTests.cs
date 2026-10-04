using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.CallChainAnalyzer, StyleBro.CodeFixes.Layout.CallChainCodeFixProvider>;

namespace StyleBro.Tests;

public class CallChainTests
{
    [Fact]
    public Task EveryCallAfterTheFirstLine_StartsItsLine() => VerifyFixAsync(
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public List<string> M(IEnumerable<string> people) =>
                people
                    .Where(p => p.Length > 1){|BRO1523:.|}Select(p => p.Trim())   {|BRO1523:.|}ToList();
        }
        """,
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public List<string> M(IEnumerable<string> people) =>
                people
                    .Where(p => p.Length > 1)
                    .Select(p => p.Trim())
                    .ToList();
        }
        """);

    [Fact]
    public Task TheFirstLine_MayHoldCalls() => VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items)
            {
                return items.Where(i => i > 0).Select(i => i * 2)
                    .OrderBy(i => i){|BRO1523:.|}ToArray();
            }
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items)
            {
                return items.Where(i => i > 0).Select(i => i * 2)
                    .OrderBy(i => i)
                    .ToArray();
            }
        }
        """);

    [Fact]
    public Task MembersLeadingToACall_StayWithIt_ATrailingPropertyToo() => VerifyFixAsync(
        """
        public class Config
        {
            public Config WriteTo => this;
            public Config MinimumLevel => this;
            public string Name => "";
            public Config Sink(int x) => this;
            public Config Debug() => this;
            public Config Create() => this;
        }

        public class C
        {
            public string M() =>
                new Config()
                    .MinimumLevel.Debug(){|BRO1523:.|}WriteTo.Sink(1)
                    .Create().Name;
        }
        """,
        """
        public class Config
        {
            public Config WriteTo => this;
            public Config MinimumLevel => this;
            public string Name => "";
            public Config Sink(int x) => this;
            public Config Debug() => this;
            public Config Create() => this;
        }

        public class C
        {
            public string M() =>
                new Config()
                    .MinimumLevel.Debug()
                    .WriteTo.Sink(1)
                    .Create().Name;
        }
        """);

    [Fact]
    public Task TheIndentation_IsTheChainsOwn() => VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items)
            {
                var a = items.Where(i => i > 0)
                             .Select(i => i){|BRO1523:.|}ToArray();
        		var b = items
        			.Where(i => i > 0){|BRO1523:.|}ToArray();
                return items.Where(i => i > 1).Select(i => i +
                    1){|BRO1523:.|}Skip(1)
                        .ToArray();
            }
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items)
            {
                var a = items.Where(i => i > 0)
                             .Select(i => i)
                             .ToArray();
        		var b = items
        			.Where(i => i > 0)
        			.ToArray();
                return items.Where(i => i > 1).Select(i => i +
                    1)
                        .Skip(1)
                        .ToArray();
            }
        }
        """);

    [Fact]
    public Task ConditionalAccess_IsALink() => VerifyFixAsync(
        """
        public class C
        {
            public string M(string s) =>
                s?.Trim()
                    ?.ToUpper(){|BRO1523:?|}.ToLower(){|BRO1523:.|}Trim();
        }
        """,
        """
        public class C
        {
            public string M(string s) =>
                s?.Trim()
                    ?.ToUpper()
                    ?.ToLower()
                    .Trim();
        }
        """);

    [Fact]
    public Task ALambdaBody_MovesWhenItsIndentationFits() => VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items) =>
                items
                    .Where(i => i > 0){|BRO1523:.|}Select(i =>
                    {
                        var j = i * 2;

                        return j;
                    }){|BRO1523:.|}ToArray();
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items) =>
                items
                    .Where(i => i > 0)
                    .Select(i =>
                    {
                        var j = i * 2;

                        return j;
                    })
                    .ToArray();
        }
        """);

    [Fact]
    public Task ALambdaBody_WithOtherIndentation_IsSkipped() => VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items) =>
                items
                    .Where(i => i > 0).Select(i =>
                {
                    return i * 2;
                })
                    .Skip(1){|BRO1523:.|}ToArray();
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public int[] M(int[] items) =>
                items
                    .Where(i => i > 0).Select(i =>
                {
                    return i * 2;
                })
                    .Skip(1)
                    .ToArray();
        }
        """);

    [Fact]
    public Task NestedChains_AreFixedTogether() => VerifyFixAsync(
        """
        using System.Linq;

        public class C
        {
            public int[][] M(int[][] items) =>
                items
                    .Where(i => i.Length > 0){|BRO1523:.|}Select(i => i
                        .Where(j => j > 0){|BRO1523:.|}ToArray())
                    .ToArray();
        }
        """,
        """
        using System.Linq;

        public class C
        {
            public int[][] M(int[][] items) =>
                items
                    .Where(i => i.Length > 0)
                    .Select(i => i
                        .Where(j => j > 0)
                        .ToArray())
                    .ToArray();
        }
        """);

    [Fact]
    public Task LineEndings_AreKept() => VerifyFixAsync(
        "public class C\r\n{\r\n    public string M(string s) =>\r\n        s\r\n            .Trim(){|BRO1523:.|}ToUpper();\r\n}\r\n",
        "public class C\r\n{\r\n    public string M(string s) =>\r\n        s\r\n            .Trim()\r\n            .ToUpper();\r\n}\r\n");

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        using System.Linq;
        using System.Threading.Tasks;

        public class C
        {
            public async Task<string> M(int[] items, string s, string[][] grid)
            {
                // Not split: on one line, or only arguments span lines.
                var a = items.Where(i => i > 0).Select(i => i).ToArray();
                await Task.Run(() =>
                {
                    a = items;
                }).ConfigureAwait(false);

                // The first member after the receiver; members without a call after the last call.
                var b = new[]
                {
                    1,
                }.Select(i => i)
                    .ToArray().Length;
                var c = grid
                    .First()[0].Length;
                var d = s
                    .Length.ToString();

                // A comment in the gap; '#if' in the chain; an interpolated string.
                var e = items
                    .Where(i => i > 0) /* why */ .ToArray();
                var f = items
                    .Where(i => i > 0)
        #if DEBUG
                    .Skip(1)
        #endif
                    .Select(i => i).ToArray();
                var g = $"{items
                    .Where(i => i > 0).Count()}";
                return s + a + b + c + d + e + f + g;
            }
        }
        """);

    [Fact]
    public Task SyntaxErrors_AreSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public string M(string s) =>
                s
                    .Trim().ToUpper({|CS1026:;|}
        }
        """);
}
