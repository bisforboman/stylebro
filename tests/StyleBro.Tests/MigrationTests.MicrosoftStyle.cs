using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>The Microsoft style in init and the migration (docs/proposals/microsoft-style.md, docs/styles.md).</summary>
public sealed partial class MigrationTests
{
    private const string RuntimeShaped = """
        using System;
        using System.Collections.Generic;

        namespace App;

        internal sealed class Cache
        {
            private static readonly object s_lock = new();
            private static int s_count;
            private readonly Dictionary<string, int> _items = new();
            private int _hits;
            private int _misses;

            public int Count => _items.Count;

            public int Get(string key)
            {
                if (_items.TryGetValue(key, out var value))
                {
                    _hits++;
                    return value;
                }

                _misses++;
                s_count++;
                return Find(key, s_lock);
            }

            private int Find(string key, object gate) => _hits + _misses + key.Length + gate.GetHashCode();
        }

        """;

    [Fact]
    public void InitWithTheStyle_PinsIt_AndWritesItsBuiltInRules()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/App/Cache.cs", RuntimeShaped);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--style", "microsoft" })));
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains($"<StyleBroPreset>{PresetVersions.Latest}</StyleBroPreset>\n    <StyleBroStyle>microsoft</StyleBroStyle>", props, StringComparison.Ordinal);
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0003.severity = warning", editorConfig, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.CA1507.severity = warning", editorConfig, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none", editorConfig, StringComparison.Ordinal);

        // The code follows the style: its '_' and 's_' fields are the defaults, nothing to write for them.
        Assert.DoesNotContain(FieldNames.StyleKey, editorConfig, StringComparison.Ordinal);
        Assert.DoesNotContain(FieldNames.StaticStyleKey, editorConfig, StringComparison.Ordinal);

        // A second run changes nothing.
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--style", "microsoft" })));
        Assert.Equal(props, File.ReadAllText(Path.Combine(root, "Directory.Build.props")));
        Assert.Equal(editorConfig, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void InitWithAnUnknownStyle_Fails()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var error = CaptureError(() => Assert.Equal(1, InitCommand.Run(new[] { root, "--style", "google" })));
        Assert.Contains("Unknown style: 'google'", error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void TheMigration_RejectsAStyle_AndRemovesOne()
    {
        var error = CaptureError(() => Capture(() => Assert.Equal(1, Program.Main(new[] { root, "--write", "--style", "microsoft" }))));
        Assert.Contains("--style is for 'stylebro-migrate init'", error, StringComparison.Ordinal);

        var props = "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n    <StyleBroStyle>microsoft</StyleBroStyle>\n  </PropertyGroup>\n</Project>\n";
        Assert.Equal("<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n", Migration.RemoveStyle(props));
        Assert.Null(Migration.RemoveStyle("<Project />"));
    }

    [Fact]
    public void SetStyle_GoesBelowThePreset_Once()
    {
        var props = "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n";
        var styled = Migration.SetStyle(props, "microsoft")!;
        Assert.Equal("<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n    <StyleBroStyle>microsoft</StyleBroStyle>\n  </PropertyGroup>\n</Project>\n", styled);
        Assert.Null(Migration.SetStyle(styled, "microsoft"));
        Assert.Contains("<StyleBroStyle>microsoft</StyleBroStyle>", Migration.SetStyle(styled.Replace("microsoft", "none", StringComparison.Ordinal), "microsoft"), StringComparison.Ordinal);
        Assert.Contains("<StyleBroStyle>microsoft</StyleBroStyle>", Migration.SetStyle("<Project>\n  <PropertyGroup>\n  </PropertyGroup>\n</Project>\n", "microsoft"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(2, 40, null)]                       // '_' fields: the style's default
    [InlineData(40, 2, "camelCase")]                // the code's own form wins
    public void TheStyle_IsTheConventionsDefault(int plain, int underscore, string? written)
    {
        var style = InitCommand.StyleSettings("microsoft")!;
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [FieldNames.StyleKey] = new[] { plain, underscore } }, new Dictionary<string, string>(), style: style).Lines;
        Assert.Equal(written is null ? [] : [$"{FieldNames.StyleKey} = {written}"], lines.Where(l => !l.StartsWith('#')));
    }

    [Fact]
    public void TheStyle_SkipsConventionsOfRulesItTurnsOff_AndKeepsUnprefixedStatics()
    {
        var style = InitCommand.StyleSettings("microsoft")!;
        var counts = new Dictionary<string, int[]>
        {
            ["stylebro_trailing_comma"] = new[] { 5, 30 },        // BRO1401 is off in the style: nothing to keep
            [Conventions.StaticPrefixKey] = new[] { 30, 0 },     // no 's_': the unprefixed names stay
            [Conventions.ThisQualificationKey] = new[] { 3, 60 }, // 'this.' everywhere: IDE0003 off
        };
        var lines = Conventions.Decide(counts, new Dictionary<string, string>(), style: style).Lines.Where(l => !l.StartsWith('#')).ToList();
        Assert.Equal([$"{FieldNames.StaticStyleKey} = PascalCase", $"{FieldNames.MutableStaticStyleKey} = _camelCase", "dotnet_diagnostic.IDE0003.severity = none"], lines);

        // Without the style, 'this.' isn't judged (IDE0003 is the style's) and the trailing comma is.
        lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines;
        Assert.DoesNotContain(lines, l => l.Contains("IDE0003", StringComparison.Ordinal));
        Assert.Contains("stylebro_trailing_comma = omit", lines);
    }

    [Fact]
    public void ThisUses_AreCounted_WithoutTheOnesAParameterNeeds()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("""
            class C
            {
                private int count;
                private int other;

                C(int count) { this.count = count; }

                int M(C c) { this.other++; return this.count + other + c.count + Get(); }

                int Get() => this.other;
            }
            """), counts);
        Assert.Equal(new[] { 2, 3 }, counts[Conventions.ThisQualificationKey]);
    }

    [Fact]
    public void Init_SuggestsTheStyle_ForRuntimeShapedCode()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"src/App/Cache{i}.cs", RuntimeShaped.Replace("class Cache", $"class Cache{i}", StringComparison.Ordinal));
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains("try 'stylebro-migrate init --style microsoft'", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Directory.Build.props")));

        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--style", "microsoft" })));
        Assert.DoesNotContain("try 'stylebro-migrate init --style microsoft'", output, StringComparison.Ordinal);
        Assert.Null(Conventions.StyleSuggestion(new Dictionary<string, int[]> { [FieldNames.StyleKey] = new[] { 0, 20 }, [Conventions.StaticPrefixKey] = new[] { 0, 10 }, [Conventions.ThisQualificationKey] = new[] { 2, 30 } }));
    }

    [Fact]
    public void ModernizeWithTheStyle_LeavesTheStylesRulesToIt()
    {
        var block = InitCommand.Modernize(root, [], styleKeys: ["dotnet_diagnostic.IDE0056.severity"]).Block!;
        Assert.DoesNotContain("IDE0056", block, StringComparison.Ordinal);
        Assert.Contains("IDE0057", InitCommand.Modernize(root, []).Block!, StringComparison.Ordinal);
    }
}
