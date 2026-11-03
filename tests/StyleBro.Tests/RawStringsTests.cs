using StyleBro.Host;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// The raw string literals a fix must not change (trial 66, moq4: IDE0055's formatter added a space to two lines of a
/// <c>$$"""</c> literal in every run, so the format never converged and the program's text changed).
/// </summary>
public sealed class RawStringsTests
{
    // moq4's MockSetupGeneratorTests.Usage, as the fix for BRO1521 leaves it (the arrow on the literal's line).
    private const string Start = "class C\n{\n    static string Usage(string type)\n        => $$\"\"\"\n";

    [Fact]
    public void ALiteralThatSaysTheSame_IsTheSame()
    {
        Assert.True(RawStrings.Same(Source(), Source()));
    }

    [Fact]
    public void AnExtraSpace_OnTheFirstLine_ChangesTheText()
    {
        Assert.False(RawStrings.Same(Source(), Source(firstLine: "         static class {{name}}")));
    }

    [Fact]
    public void ATrailingSpace_OnALine_ChangesTheText()
    {
        Assert.False(RawStrings.Same(Source(), Source(closingBraceLine: "        } ")));
    }

    [Fact]
    public void ALiteralThatMovesAsAWhole_SaysTheSame()
    {
        // The whole literal, closing quotes included, two columns further in.
        var moved = Start.Replace("    static", "  static", StringComparison.Ordinal)
            + "          static class {{name}}\n          {\n              static object Run() => Moq.Mock.Of<{{type}}>();\n          }\n          \"\"\";\n}\n";
        Assert.True(RawStrings.Same(Source(), moved));
    }

    [Fact]
    public void AnInterpolationHoleThatIsReformatted_SaysTheSame()
    {
        var plain = "class C { static string S(string a) =>\n    $$\"\"\"\n    x {{a}} y\n    \"\"\"; }\n";
        var spaced = "class C { static string S(string a) =>\n    $$\"\"\"\n    x {{ a }} y\n    \"\"\"; }\n";
        Assert.True(RawStrings.Same(plain, spaced));
    }

    [Fact]
    public void LiteralsThatSwapPlaces_SayTheSame()
    {
        var one = "class C { static string A =\n    \"\"\"\n    a\n    \"\"\";\n  static string B =\n    \"\"\"\n    b\n    \"\"\"; }\n";
        var two = "class C { static string B =\n    \"\"\"\n    b\n    \"\"\";\n  static string A =\n    \"\"\"\n    a\n    \"\"\"; }\n";
        Assert.True(RawStrings.Same(one, two));
    }

    [Fact]
    public void ACodeChange_OutsideTheLiterals_IsTheSame()
    {
        Assert.True(RawStrings.Same(Source(), Source().Replace("static string", "private static string", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task AFixThatChangesALiteral_IsKeptOutOfTheSolution()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("P", Microsoft.CodeAnalysis.LanguageNames.CSharp);
        var before = workspace.AddDocument(project.Id, "A.cs", Microsoft.CodeAnalysis.Text.SourceText.From(Source())).Project.Solution;
        var id = before.Projects.Single().DocumentIds.Single();
        var corrupted = before.WithDocumentText(id, Microsoft.CodeAnalysis.Text.SourceText.From(Source(firstLine: "         static class {{name}}")));
        var fine = before.WithDocumentText(id, Microsoft.CodeAnalysis.Text.SourceText.From(Source().Replace("static string", "private static string", StringComparison.Ordinal)));

        var kept = await RawStrings.KeepAsync(before, corrupted, "IDE0055", "the fix for IDE0055", _ => { }, CancellationToken.None);
        var allowed = await RawStrings.KeepAsync(before, fine, "BRO1404", "the fix for BRO1404", _ => { }, CancellationToken.None);

        var converted = await RawStrings.KeepAsync(before, corrupted, "BRO1138", "the fix for BRO1138", _ => { }, CancellationToken.None);

        Assert.Equal(Source(), (await kept.GetDocument(id)!.GetTextAsync()).ToString());
        Assert.Equal(Source(firstLine: "         static class {{name}}"), (await converted.GetDocument(id)!.GetTextAsync()).ToString());
        Assert.Equal(Source().Replace("static string", "private static string", StringComparison.Ordinal), (await allowed.GetDocument(id)!.GetTextAsync()).ToString());
    }

    [Fact]
    public void AKeptFile_IsAGap_SoFormatAndCheckEndNotVerified()
    {
        var line = RawStrings.GapLine(Path.Combine(Environment.CurrentDirectory, "src", "A.cs"), "the fix for IDE0055");

        Assert.Equal("Not checked: src/A.cs: the fix for IDE0055 would change the text of a raw string literal, so the file was left as it was", line);
        Assert.True(FormatCommand.IsGapLine(line));
        Assert.Single(FormatCommand.Unverified([line]));
        Assert.StartsWith("Not checked: /elsewhere/A.cs: ", RawStrings.GapLine("/elsewhere/A.cs", "x"), StringComparison.Ordinal);
    }

    private static string Source(string firstLine = "        static class {{name}}", string closingBraceLine = "        }", string callLine = "            static object Run() => Moq.Mock.Of<{{type}}>();") =>
        Start + firstLine + "\n        {\n" + callLine + "\n" + closingBraceLine + "\n        \"\"\";\n}\n";
}
