using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers;

namespace StyleBro.Tests;

public sealed class PresetVersionTests
{
    private static readonly Version Base = new(1, 0);

    [Fact]
    public void EveryRule_IsInTheLayerOfTheReleaseThatAddedIt()
    {
        var added = AddedIn();
        var inLayers = Presets.Layers.SelectMany(layer => Presets.Severities(layer.Lines).Select(p => (Id: p.Key, layer.Version))).ToList();

        Assert.Equal(inLayers.Count, inLayers.Select(p => p.Id).Distinct().Count());
        Assert.Equal(Rules().Select(d => d.Id).Order(StringComparer.Ordinal), inLayers.Select(p => p.Id).Order(StringComparer.Ordinal));
        Assert.All(inLayers, p => Assert.Equal((p.Id, added[p.Id]), (p.Id, p.Version)));
    }

    [Fact]
    public void RulesAfterTheBase_AreOffByDefault()
    {
        // Mechanism 1 of docs/proposals/preset-versions.md: without a preset that names them (StyleBroPreset=none, a
        // migration's block), a newer minor's rules stay off.
        var added = AddedIn();
        Assert.All(Rules().Where(d => added[d.Id] > Base), d => Assert.False(d.IsEnabledByDefault, $"{d.Id} is new after {Base}: isEnabledByDefault must be false."));
    }

    [Fact]
    public void NoKeyIsInTwoLayers()
    {
        // Two global configs at the same global_level that set one key conflict, and the compiler drops the key.
        var keys = Presets.Layers.SelectMany(layer => layer.Lines
            .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
            .Select(l => l[..l.IndexOf('=')].Trim())
            .Where(k => k is not "is_global" and not "global_level")
            .Distinct()).ToList();
        Assert.Empty(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key));
        Assert.All(Presets.Layers, layer => Assert.Contains("is_global = true", layer.Lines));
    }

    [Fact]
    public void TheTargets_AddEveryLayer_AndAgreeOnLatest()
    {
        var targets = File.ReadAllText(Path.Combine(Presets.Root, "src", "StyleBro.Package", "build", "StyleBro.Analyzers.targets"));
        Assert.Equal(Presets.Layers.Select(l => l.Version), Regex.Matches(targets, @"<EditorConfigFiles Include=""[^""]*presets/stylebro\.(\d+\.\d+)\.globalconfig""").Select(m => Version.Parse(m.Groups[1].Value)));
        foreach (var layer in Presets.Layers.Skip(1))
        {
            Assert.Contains($"presets/stylebro.{layer.Version}.globalconfig\" Condition=\"$([MSBuild]::VersionGreaterThanOrEquals('$(_StyleBroPresetLevel)', '{layer.Version}'))\"", targets, StringComparison.Ordinal);
        }

        Assert.Equal(Presets.Layers[^1].Version, Version.Parse(PresetVersions.Latest));
        Assert.Contains($"<_StyleBroPresetLatest>{PresetVersions.Latest}</_StyleBroPresetLatest>", targets, StringComparison.Ordinal);
        Assert.Equal(Base, Presets.Layers[0].Version);
        Assert.Contains($"<_StyleBroPresetDefault>{Base}</_StyleBroPresetDefault>", targets, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStyleLayers_AreAboveThePresetLayers()
    {
        // Same-level global configs that set one key conflict (the key is dropped): a style overrides preset keys, so the
        // preset sits at -2 and the styles at -1; the repository's own files (level 0 and up, .editorconfig) win over both.
        Assert.All(Presets.Layers, layer => Assert.Contains("global_level = -2", layer.Lines));
        Assert.NotEmpty(Presets.Styles);
        Assert.All(Presets.Styles, layer => Assert.Contains("global_level = -1", layer.Lines));
        Assert.All(Presets.Styles, layer => Assert.Contains("is_global = true", layer.Lines));
    }

    [Fact]
    public void EveryStyleLayer_BelongsToAPresetLayer_AndNamesOnlyItsRulesAndKnownKeys()
    {
        var configuration = File.ReadAllText(Path.Combine(Presets.Root, "docs", "configuration.md"));
        foreach (var style in Presets.Styles.GroupBy(s => s.Name))
        {
            var keys = style.SelectMany(layer => Presets.Keys(layer.Lines)).ToList();
            Assert.Empty(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key));
            foreach (var layer in style)
            {
                Assert.Contains(Presets.Layers, p => p.Version == layer.Version);

                // A style decides only rules the preset versions up to its own have (they turn them on or off).
                var known = Presets.Layers.Where(p => p.Version <= layer.Version).SelectMany(p => Presets.Severities(p.Lines)).Select(p => p.Key).ToHashSet();
                Assert.All(Presets.Severities(layer.Lines), p => Assert.Contains(p.Key, known));

                // Severities of built-in rules belong in init's .editorconfig block ('dotnet format' ignores them here).
                Assert.DoesNotContain(layer.Lines, l => l.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) && !l.StartsWith("dotnet_diagnostic.BRO", StringComparison.Ordinal));

                // Options: StyleBro's (documented) or the SDK's; never the using sort key, which can't be unset.
                foreach (var key in Presets.Keys(layer.Lines).Where(k => !k.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal)))
                {
                    Assert.True(key.StartsWith("stylebro_", StringComparison.Ordinal) ? configuration.Contains($"`{key}`", StringComparison.Ordinal) : key.StartsWith("csharp_", StringComparison.Ordinal) || key.StartsWith("dotnet_", StringComparison.Ordinal), key);
                    Assert.NotEqual("dotnet_sort_system_directives_first", key);
                }
            }
        }
    }

    [Fact]
    public void TheTargets_AddEveryStyleLayer_UpToThePresetLevel()
    {
        var targets = File.ReadAllText(Path.Combine(Presets.Root, "src", "StyleBro.Package", "build", "StyleBro.Analyzers.targets"));
        Assert.Equal(
            Presets.Styles.Select(s => $"{s.Name}.{s.Version}").Order(StringComparer.Ordinal),
            Regex.Matches(targets, @"<EditorConfigFiles Include=""[^""]*presets/styles/(\w+\.\d+\.\d+)\.globalconfig""").Select(m => m.Groups[1].Value).Order(StringComparer.Ordinal));
        foreach (var layer in Presets.Styles.Where(s => s.Version > Base))
        {
            Assert.Contains($"presets/styles/{layer.Name}.{layer.Version}.globalconfig\" Condition=\"$([MSBuild]::VersionGreaterThanOrEquals('$(_StyleBroPresetLevel)', '{layer.Version}'))\"", targets, StringComparison.Ordinal);
        }

        Assert.Contains("Code=\"STYLEBRO003\"", targets, StringComparison.Ordinal);
        Assert.Contains("Code=\"STYLEBRO004\"", targets, StringComparison.Ordinal);
    }

    /// <summary>
    /// The version each rule was added in, from AnalyzerReleases.Shipped.md's release headers (major.minor; everything before
    /// 1.0 is the base layer). AnalyzerReleases.Unshipped.md's rules go into the next release: the newest layer.
    /// </summary>
    private static Dictionary<string, Version> AddedIn()
    {
        var folder = Path.Combine(Presets.Root, "src", "StyleBro.Analyzers");
        var result = new Dictionary<string, Version>(StringComparer.Ordinal);
        Version? release = null;
        var newRules = false;
        foreach (var line in File.ReadAllLines(Path.Combine(folder, "AnalyzerReleases.Shipped.md")))
        {
            if (Regex.Match(line, @"^## Release (\d+)\.(\d+)") is { Success: true } header)
            {
                var version = new Version(int.Parse(header.Groups[1].Value), int.Parse(header.Groups[2].Value));
                release = version < Base ? Base : version;
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                newRules = line == "### New Rules";
            }
            else if (newRules && Regex.Match(line, @"^(BRO\d{4})\s*\|") is { Success: true } row)
            {
                result[row.Groups[1].Value] = release!;
            }
        }

        // The next release is newer than every shipped one (once releases are past the base).
        var latest = Version.Parse(PresetVersions.Latest);
        Assert.True(result.Values.All(v => v <= latest), $"PresetVersions.Latest ({latest}) is older than a shipped release.");
        foreach (var line in File.ReadAllLines(Path.Combine(folder, "AnalyzerReleases.Unshipped.md")))
        {
            if (Regex.Match(line, @"^(BRO\d{4})\s*\|") is { Success: true } row)
            {
                result[row.Groups[1].Value] = latest;
            }
        }

        return result;
    }

    // Every configurable rule (not the hidden companions like BRO1405_p).
    private static IEnumerable<DiagnosticDescriptor> Rules() => typeof(DiagnosticIds).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
        .SelectMany(t => ((DiagnosticAnalyzer)Activator.CreateInstance(t)!).SupportedDiagnostics)
        .Where(d => Regex.IsMatch(d.Id, @"^BRO\d{4}$"))
        .GroupBy(d => d.Id)
        .Select(g => g.First());
}

/// <summary>The preset's layers (src/StyleBro.Package/build/presets): one global config per minor release.</summary>
internal static class Presets
{
    public static string Root { get; } = FindRoot();

    /// <summary>Gets the layers in version order: (major.minor, lines).</summary>
    public static IReadOnlyList<(Version Version, string[] Lines)> Layers { get; } = Directory
        .GetFiles(Path.Combine(Root, "src", "StyleBro.Package", "build", "presets"), "*.globalconfig")
        .Select(file => (Version: Version.Parse(Regex.Match(Path.GetFileName(file), @"^stylebro\.(\d+\.\d+)\.globalconfig$").Groups[1].Value), Lines: File.ReadAllLines(file)))
        .OrderBy(layer => layer.Version)
        .ToList();

    /// <summary>Gets the styles' layers (presets/styles/name.X.Y.globalconfig): (style, major.minor, lines).</summary>
    public static IReadOnlyList<(string Name, Version Version, string[] Lines)> Styles { get; } = Directory
        .GetFiles(Path.Combine(Root, "src", "StyleBro.Package", "build", "presets", "styles"), "*.globalconfig")
        .Select(file => (Match: Regex.Match(Path.GetFileName(file), @"^(\w+)\.(\d+\.\d+)\.globalconfig$"), Lines: File.ReadAllLines(file)))
        .Select(p => (Name: p.Match.Groups[1].Value, Version: Version.Parse(p.Match.Groups[2].Value), p.Lines))
        .OrderBy(layer => layer.Name, StringComparer.Ordinal)
        .ThenBy(layer => layer.Version)
        .ToList();

    /// <summary>Gets the ids the latest preset turns on (severity other than none).</summary>
    public static IReadOnlySet<string> TurnedOn { get; } = Severities(AllLines())
        .Where(p => p.Value != "none")
        .Select(p => p.Key)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Every layer's lines: the preset 'latest' gives.</summary>
    public static string[] AllLines() => Layers.SelectMany(layer => layer.Lines).ToArray();

    /// <summary>Lines that turn on the rules off by default that the latest preset turns on (none until a minor adds rules).</summary>
    public static string EnableLines(IEnumerable<DiagnosticDescriptor> descriptors) => string.Concat(descriptors
        .Where(d => !d.IsEnabledByDefault && TurnedOn.Contains(d.Id))
        .Select(d => $"dotnet_diagnostic.{d.Id}.severity = warning\n"));

    public static IEnumerable<KeyValuePair<string, string>> Severities(IEnumerable<string> lines) => lines
        .Select(l => Regex.Match(l, @"^dotnet_diagnostic\.(BRO\d{4})\.severity\s*=\s*(\w+)"))
        .Where(m => m.Success)
        .Select(m => KeyValuePair.Create(m.Groups[1].Value, m.Groups[2].Value));

    /// <summary>The keys a global config sets (not is_global/global_level).</summary>
    public static IEnumerable<string> Keys(IEnumerable<string> lines) => lines
        .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
        .Select(l => l[..l.IndexOf('=')].Trim())
        .Where(k => k is not "is_global" and not "global_level");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "StyleBro.slnx")))
        {
            directory = directory.Parent!;
        }

        return directory.FullName;
    }
}
