using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Host;

namespace StyleBro.Tests;

/// <summary>
/// The host's merged fix rounds (docs/proposals/format-speed.md, phase 3): which ids' fixes meet comes from the fix-order cases,
/// not from a hand-kept list. <see cref="TheList_IsWhatTheFixOrderCasesSay"/> derives it from FixOrderTests (the ids one case
/// fixes together interact; a rule of the Naming area renames) and compares it with src/StyleBro.FormatHost/FixInteractions.txt,
/// which the host embeds: a new rule, or a new case, fails it until the list is written again
/// (STYLEBRO_WRITE_INTERACTIONS=1 dotnet test --filter FixInteractionsTests).
/// </summary>
public sealed class FixInteractionsTests
{
    private const string WriteVariable = "STYLEBRO_WRITE_INTERACTIONS";

    [Fact]
    public void TheList_IsWhatTheFixOrderCasesSay()
    {
        var path = Path.Combine(RepositoryRoot(), "src", "StyleBro.FormatHost", "FixInteractions.txt");
        var expected = Expected();
        if (Environment.GetEnvironmentVariable(WriteVariable) == "1")
        {
            File.WriteAllText(path, expected);
        }

        var actual = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
        Assert.True(actual == expected, $"src/StyleBro.FormatHost/FixInteractions.txt isn't what the fix-order cases and the rules say: write it again with {WriteVariable}=1 dotnet test --filter FixInteractionsTests (a new rule whose fix meets another's needs a FixOrderTests case first).");

        // The host reads the copy it embeds.
        Assert.Equal(FixInteractions.Parse(expected.Split('\n')).Ids, FixInteractions.Default.Ids);
    }

    [Fact]
    public void EveryRule_IsClassified()
    {
        var list = FixInteractions.Default;
        Assert.All(RuleIds().Keys, id => Assert.True(list.Knows(id), $"{id} isn't in FixInteractions.txt"));
        Assert.True(list.Renames("BRO1303"));
        Assert.False(list.Renames("BRO1001"));
    }

    [Fact]
    public void Ids_InteractWithinACase_AndUnknownOnesWithAll()
    {
        var list = FixInteractions.Parse(["# a comment", "A1 B1", "B1", "C1", "R1 rename"]);
        Assert.True(list.Interact("A1", "B1"));
        Assert.True(list.Interact("B1", "A1"));
        Assert.False(list.Interact("A1", "C1"));
        Assert.True(list.Interact("C1", "R1"));
        Assert.True(list.Interact("C1", "IDE0011"));
        Assert.True(list.Renames("R1"));
        Assert.Equal(["A1", "B1", "C1", "R1"], list.Ids);
        Assert.Equal("# h\nA1 B1\nB1 A1\nC1\nR1 rename\n", FixInteractions.Format(["R1", "C1", "B1", "A1"], [["A1", "B1", "X9"]], ["R1"], ["h"]));
    }

    [Fact]
    public void TheRounds_KeepTheOrder_OfIdsThatInteract()
    {
        // A1-B1 and B1-D1 interact; C1 and E1 don't; Z1 isn't in the list; R1 and R2 rename.
        var list = FixInteractions.Parse(["A1 B1", "B1 A1 D1", "C1", "D1 B1", "E1", "R1 rename", "R2 rename"]);
        var rounds = FixRounds.Of(["R2", "E1", "D1", "C1", "B1", "A1", "R1", "A1"], list);
        Assert.Equal([["A1", "C1", "E1"], ["B1"], ["D1"], ["R1"], ["R2"]], rounds);

        // An id the list doesn't know waits for every id before it, and every id after it waits for it.
        Assert.Equal([["A1", "C1"], ["Y1"], ["Z1"], ["R1"]], FixRounds.Of(["A1", "C1", "Y1", "Z1", "R1"], list));
        Assert.Empty(FixRounds.Of([], list));
    }

    [Fact]
    public async Task ARound_MergesEditsThatDontMeet_AndDefersTheRest()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.CurrentSolution.AddProject("P", "P", LanguageNames.CSharp);
        var a = project.AddDocument("A.cs", SourceText.From("class A { int x; int y; }", Encoding.UTF8));
        var start = a.Project.Solution;
        var id = a.Id;
        Solution Edit(int at, int length, string text) => start.WithDocumentText(id, start.GetDocument(id)!.GetTextAsync().Result.WithChanges(new TextChange(new TextSpan(at, length), text)));

        // 'x' to 'xx' and 'y' to 'yy' merge; a second edit of 'x', and an insertion right after 'y', meet those: deferred.
        var (merged, deferred) = await FixRounds.MergeAsync(start, [("X1", Edit(14, 1, "xx")), ("Y1", Edit(21, 1, "yy")), ("X2", Edit(14, 1, "z")), ("Y2", Edit(22, 0, "2"))], CancellationToken.None);
        Assert.Equal("class A { int xx; int yy; }", (await merged.GetDocument(id)!.GetTextAsync()).ToString());
        Assert.Equal(["X2", "Y2"], deferred);

        // A fix that adds a document, or changes the encoding, isn't merged.
        var added = start.AddDocument(DocumentId.CreateNewId(a.Project.Id), "B.cs", "class B { }");
        var encoded = start.WithDocumentText(id, SourceText.From("class A { int x; int y; }", Encoding.Unicode));
        var (same, both) = await FixRounds.MergeAsync(start, [("N1", added), ("E1", encoded)], CancellationToken.None);
        Assert.Equal(["N1", "E1"], both);
        Assert.Same(start, same);
    }

    // The ids each fix-order case fixes together: the string arguments of its AssertConvergesInEveryOrder* call that name a rule.
    internal static List<List<string>> Cases()
    {
        var file = Path.Combine(RepositoryRoot(), "tests", "StyleBro.Tests", "FixOrderTests.cs");
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Preview)).GetRoot();
        var rules = RuleIds();
        return root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is IdentifierNameSyntax { Identifier.Text: var name } && name.StartsWith("AssertConvergesInEveryOrder", StringComparison.Ordinal))
            .Where(i => i.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() is not { } method || !method.Identifier.Text.StartsWith("AssertConvergesInEveryOrder", StringComparison.Ordinal))
            .Select(i => i.ArgumentList.Arguments.Select(a => a.Expression).OfType<LiteralExpressionSyntax>().Select(l => l.Token.ValueText).Where(rules.ContainsKey).Distinct(StringComparer.Ordinal).ToList())
            .Where(ids => ids.Count > 1)
            .ToList();
    }

    // Every StyleBro rule id and its category.
    private static Dictionary<string, string> RuleIds() =>
        typeof(StyleBro.Analyzers.DiagnosticIds).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .SelectMany(t => ((DiagnosticAnalyzer)Activator.CreateInstance(t)!).SupportedDiagnostics)
            .Where(d => d.Id.StartsWith("BRO", StringComparison.Ordinal) && d.Id.Length == 7)
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Category, StringComparer.Ordinal);

    private static string Expected()
    {
        var rules = RuleIds();
        return FixInteractions.Format(
            rules.Keys,
            Cases(),
            rules.Where(r => r.Value == "Naming").Select(r => r.Key),
            [
                "Which StyleBro ids' fixes meet, for the format host's merged fix rounds (docs/format-engine.md). Written by",
                "FixInteractionsTests from tests/StyleBro.Tests/FixOrderTests.cs: two ids one case fixes together interact; a rule of",
                "the Naming area renames ('rename': it interacts with every id). Don't edit it: add a FixOrderTests case and run",
                $"{WriteVariable}=1 dotnet test --filter FixInteractionsTests.",
            ]);
    }

    private static string RepositoryRoot()
    {
        var folder = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(folder, "StyleBro.slnx")))
        {
            folder = Path.GetDirectoryName(folder) ?? throw new InvalidOperationException("No StyleBro.slnx above the test's folder.");
        }

        return folder;
    }
}
