using System.Text.RegularExpressions;
using StyleBro.Analyzers.Baseline;
using static StyleBro.Tests.DocExamplesTests;

namespace StyleBro.Tests;

/// <summary>
/// Every rule on, every fix applied: no comment may be lost or doubled. A fix that removes or rewrites tokens can drop
/// the comments around them (BRO1405/BRO1410 lost '// Contains delimiter' in CsvHelper).
/// </summary>
public class CommentPreservationTests
{
    // Every comment carries a marker c01..cNN; each must survive exactly once.
    private const string Code = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace App
        {
            /// <summary>The kinds. c01</summary>
            public enum Kind { A /* c02 */, B // c03
            }

            [Serializable/* c04 */, Obsolete /* c05 */]
            public class C : object // c06
            {
                private int Count = 1, other = 2; // c07
                private readonly int[] items = new int[] { 1, 2 /* c08 */ };

                public int P { get /* c09 */ { return Count; } set { Count = value; } }

                public C(int a, int b) : base() // c10
                {
                    other = (a + b /* c11 */);
                }

                public int Add(int x)
                {
                    return (
                        x + 1 // c12
                    );
                }

                public int Block(int x)
                {
                    return (x + 1 /* c13 */);
                }

                public bool Pattern(int x) => x is (> 1 /* c14 */) and (< 10 /* c15 */);

                public bool Quote(string field, string d)
                {
                    var shouldQuote =
                        (
                            field[0] == ' ' // c16
                            || (d.Length > 0 && field.Contains(d)) // c17
                        );
                    return shouldQuote;
                }

                public int Cast(object o) => ((int)(o /* c18 */));

                public string Cond(bool b) => (b /* c19 */) ? ("a" /* c20 */) : (""/* c21 */);

                public void Calls(List<int> list)
                {
                    if (list.Count == 0) return; // c22
                    if (null == list) { return; } /* c23 */ else if (list.Count > 1) { Console.WriteLine(1); }
                    var q = list.Where(x => x > 0 /* c24 */).Select((x) => x /* c25 */).ToList() // c26
                        .Count;
                    Func<int, int> f = delegate (int y) { return y; /* c27 */ };
                    var t = Tuple.Create(1, // c28
                        2);
                    var n = new Nullable<int>(/* c29 */);
                    var s = string.Format("{0}" /* c30 */ + "" /* c31 */, f(q));
                    var (a, b) = (1 /* c32 */, 2);
                    var e = list.Count switch
                    {
                        0 => "none", // c33
                        _ => "some" /* c34 */
                    };
                    Console.WriteLine(s + e + a + b + t + n + (q /* c35 */));
                    ;// c36
                }

                //c37
                public override string ToString() => base.ToString() /* c38 */;

                public int More(int? x, string? s, List<int> list, int y)
                {
                    if (x.HasValue /* c39 */) { }
                    if (y > 0)
                    {
                        if (y > 1 /* c40 */) // c41
                        {
                            Console.WriteLine(s! /* c42 */);
                        }
                    }
                    else
                    {
                        // c43
                        if (y < 0) { Console.WriteLine(2); }
                    }

                    if (y == 3) { return 1; } else { /* c44 */ Console.WriteLine(3); }
                    var count = list.Where(i => i > 0 /* c45 */).Count(/* c46 */);
                    var l = (long)/* c47 */1 + (long)1 /* c48 */;
                    var u = 1ul /* c49 */;
                    var tuple = new ValueTuple<int, /* c50 */ int>(1, 2);
                    var item = tuple.Item1 /* c51 */;
                    Params(new[] { 1, /* c52 */ 2 });
                    var o = new C(1, 2) /* c53 */;
                    var raw = $"text" /* c54 */ + @"verbatim" /* c55 */;
                    var query = from i in list // c56
                                where i > 0 select i /* c57 */;
                    Func<int, int> g = delegate (int /* c58 */ z) { return z; };
                    return count + item + (int)l + (int)u + raw.Length + query.Count() + g(1);
                    return 0; /* c59 */
                }

                public void Params(int[] values) /* c60 */
                {
                    return; // c61
                }
            }

            public record R() { /* c62 */ }

            public class D : /* c63 */ object
            {
                // c64
                public ValueTuple<int, /* c65 */ string> Pair { get; set; }
            }
        }
        """;

    [Fact]
    public async Task EveryFix_KeepsEveryComment()
    {
        var ids = BaselineSuppressor.BroIds().OrderBy(id => id, StringComparer.Ordinal).ToList();
        var config = "root = true\n\n[*.cs]\n" + string.Concat(ids.Select(id => $"dotnet_diagnostic.{id}.severity = warning\n"));
        var document = CreateDocument(Code, config);
        var fixedIds = new HashSet<string>();
        var losses = new List<string>();
        for (var round = 0; round < 2; round++)
        {
            foreach (var id in ids)
            {
                var analyzers = FindAnalyzer(id);
                var diagnostics = await GetDiagnosticsAsync(document, analyzers, id);
                if (diagnostics.Length > 0)
                {
                    fixedIds.Add(id);
                    var fixedDocument = await FixAllAsync(document, FindCodeFix(id), analyzers, id, diagnostics);
                    var text = (await fixedDocument.GetTextAsync()).ToString();
                    var lost = Regex.Matches(Code, @"c\d\d\b").Select(m => m.Value)
                        .Where(marker => Regex.Matches(text, marker + @"\b").Count != 1)
                        .ToList();
                    if (lost.Count > 0)
                    {
                        // Go on without this fix, so one run lists every rule that loses a comment.
                        losses.Add($"{id}: {string.Join(", ", lost)}\n{text}");
                        continue;
                    }

                    document = fixedDocument;
                }
            }
        }

        Assert.True(losses.Count == 0, string.Join("\n\n", losses));

        // The file really exercises the rules that touch these constructs.
        Assert.Contains("BRO1405", fixedIds);
        Assert.Contains("BRO1410", fixedIds);
    }
}
