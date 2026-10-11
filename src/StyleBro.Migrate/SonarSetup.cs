using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace StyleBro.Migrate;

/// <summary>
/// A repository's SonarQube/SonarAnalyzer setup (owner's decision 2026-10-09): which Sonar rules are on, so the StyleBro
/// and SDK rules that fix what they report can be turned on. The base is a quality profile exported from the server
/// (--sonar-profile), else the package's defaults ("Sonar way") when SonarAnalyzer.CSharp is referenced, else nothing; then
/// rulesets with Sonar rules (the scanner's and SonarLint's too), .globalconfig files (the strictest of several wins, like
/// for StyleCop) and the root .editorconfig's sections for all C#. Only Sonar's public rule list is used.
/// </summary>
internal sealed class SonarSetup
{
    /// <summary>The SonarAnalyzer.CSharp version the embedded rule list (and its defaults) comes from.</summary>
    public const string DefaultsVersion = "10.35.0.4138";

    private static readonly Regex DiagnosticKey = new(@"^dotnet_diagnostic\.(S\d+)\.severity$", RegexOptions.IgnoreCase);
    private static readonly Regex Package = new(@"\b(?:Include|Update)\s*=\s*""SonarAnalyzer\.CSharp""(?<rest>[^>]*)>", RegexOptions.IgnoreCase);
    private static readonly Regex Version = new(@"\bVersion\s*=\s*""(?<version>[^""]+)""", RegexOptions.IgnoreCase);

    private SonarSetup(Dictionary<string, Severity> severities, List<string> sources)
    {
        Severities = severities;
        Sources = sources;
    }

    /// <summary>Gets every Sonar C# rule: on by default ("Sonar way") and its title.</summary>
    public static IReadOnlyDictionary<string, (bool SonarWay, string Title)> Rules { get; } = LoadRules();

    /// <summary>Gets the Sonar rules a StyleBro or SDK rule fixes, in the order of the mapping file.</summary>
    public static IReadOnlyList<MappedRule> Mapping { get; } = LoadMapping().Where(m => m.Rule != "-").ToList();

    /// <summary>
    /// Gets the Sonar rules with a fixing rule that isn't safe (mode 'none' in the mapping file), with the reason: reported as
    /// not covered. S3260: CA1852 seals private nested classes that have protected constructors (CS0628 in SmartEnum).
    /// </summary>
    public static IReadOnlyList<MappedRule> NotCovered { get; } = LoadMapping().Where(m => m.Rule == "-").ToList();

    /// <summary>Gets the effective severity of every Sonar rule the setup mentions (the defaults included).</summary>
    public IReadOnlyDictionary<string, Severity> Severities { get; }

    /// <summary>Gets what the setup was read from, for the report.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>
    /// Reads the setup, or returns null when there is none: no SonarAnalyzer.CSharp reference, no profile, and no Sonar
    /// rule in a ruleset or config. <paramref name="profile"/>: a quality profile backup (XML) from the server.
    /// </summary>
    public static SonarSetup? Read(string root, string? profile = null)
    {
        var files = StyleCopSetup.EnumerateFiles(root).ToList();
        var references = StyleCopSetup.FollowReferences(root, files);
        var sources = new List<string>();
        var severities = new Dictionary<string, Severity>(StringComparer.Ordinal);

        if (profile is not null)
        {
            sources.Add($"{Path.GetFileName(profile)} (quality profile)");
            foreach (var id in ReadProfile(profile))
            {
                severities[id] = Severity.Warning;
            }
        }
        else if (PackageVersion(files.Where(StyleCopSetup.IsMSBuild).Concat(references.MSBuild)) is { } version)
        {
            sources.Add($"SonarAnalyzer.CSharp {version} (its default rules, \"Sonar way\", as of {DefaultsVersion})");
            foreach (var (id, rule) in Rules.Where(r => r.Value.SonarWay))
            {
                severities[id] = Severity.Warning;
            }
        }

        // Several rulesets or global configs (one per project type): the strictest wins, one that doesn't set a rule
        // counting with the value before it, so a test project's config doesn't turn a rule off for production code.
        var rulesets = files.Where(f => f.EndsWith(".ruleset", StringComparison.OrdinalIgnoreCase)).Concat(references.Rulesets)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(f => !Path.GetFileName(f).Contains("none", StringComparison.OrdinalIgnoreCase))
            .Select(f => (File: f, Rules: ReadRuleset(f)))
            .Where(r => r.Rules.Count > 0)
            .ToList();
        var globals = files.Where(f => f.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase))
            .Select(f => (File: f, Rules: ReadConfig(f, global: true)))
            .Where(r => r.Rules.Count > 0)
            .ToList();
        var editorConfig = Path.Combine(root, ".editorconfig");
        var rootSections = File.Exists(editorConfig) ? ReadConfig(editorConfig, global: false) : [];
        foreach (var kind in new[] { rulesets, globals })
        {
            foreach (var id in kind.SelectMany(k => k.Rules.Keys).Distinct().ToList())
            {
                var before = severities.TryGetValue(id, out var b) ? b : Severity.None;
                severities[id] = kind.Max(k => k.Rules.TryGetValue(id, out var s) ? s : before);
            }
        }

        foreach (var (id, severity) in rootSections)
        {
            severities[id] = severity;
        }

        sources.AddRange(rulesets.Concat(globals).Select(r => Path.GetRelativePath(root, r.File)));
        if (rootSections.Count > 0)
        {
            sources.Add(".editorconfig");
        }

        return sources.Count > 0 ? new SonarSetup(severities, sources) : null;
    }

    public bool IsOn(string id) => Severities.TryGetValue(id, out var severity) && severity >= Severity.Suggestion;

    /// <summary>
    /// Adds the settings for the Sonar rules that are on to <paramref name="lines"/> (an .editorconfig section): the rule
    /// that fixes each, at the strongest severity of the Sonar rules it stands for, unless it's already on at least as
    /// strongly (a rule on through StyleCop or Sonar is on), and its settings, which replace the same key's value from the
    /// StyleCop setup (listed as a note). <paramref name="ruleOn"/>: whether a rule is on without Sonar (for the 'option'
    /// rows, which only set an option of it); <paramref name="own"/>: keys the repository sets itself, left alone.
    /// </summary>
    public Applied Apply(List<string> lines, Func<string, bool> ruleOn, ISet<string> own)
    {
        var applied = new Applied { On = Severities.Count(p => p.Value >= Severity.Suggestion) };
        var added = new List<string>();
        applied.NotCovered.AddRange(NotCovered.Where(m => IsOn(m.Sonar)).Select(m => (m.Sonar, m.Note!)));
        foreach (var group in Mapping.Where(m => IsOn(m.Sonar)).GroupBy(m => m.Rule))
        {
            var rule = group.Key;
            var ids = group.Select(m => m.Sonar).Distinct().ToList();
            var settings = group.Select(m => m.Setting).OfType<string>().Distinct().ToList();
            var key = $"dotnet_diagnostic.{rule}.severity";
            var optionOnly = group.All(m => m.OptionOnly);
            string? reason = optionOnly && !ruleOn(rule) ? $"{rule} is off here, and {string.Join(", ", settings.Select(KeyOf))} is one of its options"
                : own.Contains(key) || settings.Any(s => own.Contains(KeyOf(s)!)) ? $"the repository's .editorconfig sets {rule} or its options itself"
                : null;
            if (reason is not null)
            {
                ids.ForEach(id => applied.NotApplied.Add((id, reason)));
                continue;
            }

            var section = new List<string> { "# " + string.Join("; ", ids.Select(id => $"{id}: {Title(id)}")) };
            if (!optionOnly)
            {
                var severity = ids.Max(id => Severities[id]);
                var existing = lines.FindIndex(l => KeyOf(l) == key);
                if (existing < 0 || (StyleCopSetup.ParseSeverity(ValueOf(lines[existing])) ?? Severity.None) < severity)
                {
                    if (existing >= 0)
                    {
                        lines.RemoveAt(existing);
                    }

                    section.Add($"{key} = {severity.ToString().ToLowerInvariant()}");
                }
            }

            foreach (var setting in settings)
            {
                var existing = lines.FindIndex(l => KeyOf(l) == KeyOf(setting));
                if (existing >= 0)
                {
                    if (ValueOf(lines[existing]) != ValueOf(setting))
                    {
                        applied.Notes.Add($"{KeyOf(setting)}: {ValueOf(setting)} for {string.Join(", ", ids)} instead of {ValueOf(lines[existing])} from the StyleCop setup.");
                    }

                    lines.RemoveAt(existing);
                }

                section.Add(setting);
            }

            if (section.Count > 1)
            {
                added.AddRange(section);
            }

            foreach (var row in group)
            {
                var detail = string.Join("; ", new[] { row.Setting, row.Note }.OfType<string>());
                applied.Enforced.Add((row.Sonar, rule + (detail.Length == 0 ? string.Empty : $" ({detail})")));
            }
        }

        if (added.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("# From the SonarQube setup: rules that fix what these Sonar rules report (Sonar keeps reporting them too).");
            lines.AddRange(added);
        }

        return applied;
    }

    /// <summary>The report's Sonar part, ending with an empty line (the preview prints it as one block).</summary>
    public List<string> Report(Applied applied)
    {
        JsonReport.Set("sonar", new JsonObject
        {
            ["sources"] = new JsonArray(Sources.Select(s => (JsonNode?)s).ToArray()),
            ["rulesOn"] = applied.On,
            ["fixed"] = new JsonArray(applied.Enforced.Select(e => (JsonNode?)new JsonObject { ["sonar"] = e.Sonar, ["by"] = e.Target }).ToArray()),
            ["notApplied"] = new JsonArray(applied.NotApplied.Select(n => (JsonNode?)new JsonObject { ["sonar"] = n.Sonar, ["reason"] = n.Reason }).ToArray()),
            ["notCovered"] = new JsonArray(applied.NotCovered.Select(n => (JsonNode?)new JsonObject { ["sonar"] = n.Sonar, ["reason"] = n.Reason }).ToArray()),
            ["notes"] = new JsonArray(applied.Notes.Select(n => (JsonNode?)n).ToArray()),
        });
        var report = new List<string>
        {
            $"Sonar: read {string.Join(", ", Sources)}",
            $"Sonar rules on: {applied.On}; fixed by StyleBro or the SDK from now on: {applied.Enforced.Select(e => e.Sonar).Distinct().Count()}",
        };
        report.AddRange(applied.Enforced.Select(e => $"  {e.Sonar} -> {e.Target}"));
        if (applied.NotApplied.Count > 0)
        {
            report.Add("  Not applied:");
            report.AddRange(applied.NotApplied.Select(n => $"    {n.Sonar}: {n.Reason}"));
        }

        if (applied.NotCovered.Count > 0)
        {
            report.Add("  Not covered (no safe fix):");
            report.AddRange(applied.NotCovered.Select(n => $"    {n.Sonar}: {n.Reason}"));
        }

        report.AddRange(applied.Notes.Select(n => "  " + n));
        var rest = applied.On - applied.Enforced.Select(e => e.Sonar).Distinct().Count();
        report.Add($"  The other {rest} stay Sonar's (StyleBro has no safe automatic fix for them). StyleBro doesn't turn Sonar rules off:");
        report.Add("  Sonar keeps reporting its own ids, the ones above too.");
        report.Add(string.Empty);
        return report;
    }

    private static string Title(string id) => Rules.TryGetValue(id, out var rule) ? rule.Title : id;

    private static string? KeyOf(string line)
    {
        var equals = line.IndexOf('=');
        return line.StartsWith('#') || equals <= 0 ? null : line.Substring(0, equals).Trim();
    }

    private static string ValueOf(string line) => line.Substring(line.IndexOf('=') + 1).Trim();

    /// <summary>The SonarAnalyzer.CSharp version the MSBuild files reference ('?' when only the reference names none), or null.</summary>
    private static string? PackageVersion(IEnumerable<string> msbuild)
    {
        string? found = null;
        foreach (var file in msbuild.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (Match match in Package.Matches(StyleCopSetup.WithoutComments(File.ReadAllText(file))))
            {
                var version = Version.Match(match.Groups["rest"].Value);
                found = version.Success ? version.Groups["version"].Value : found ?? "?";
            }
        }

        return found;
    }

    /// <summary>The Sonar rules a quality profile backup ('/api/qualityprofiles/backup') activates for C#.</summary>
    private static List<string> ReadProfile(string file)
    {
        return XDocument.Load(file).Descendants("rule")
            .Where(r => (string?)r.Element("repositoryKey") == "csharpsquid")
            .Select(r => ((string?)r.Element("key"))?.Trim())
            .OfType<string>()
            .Where(k => Regex.IsMatch(k, @"^S\d+$"))
            .ToList();
    }

    /// <summary>The Sonar rules of a ruleset: '&lt;Rules AnalyzerId="SonarAnalyzer.CSharp"&gt;' (the scanner's and SonarLint's too).</summary>
    private static Dictionary<string, Severity> ReadRuleset(string file)
    {
        var result = new Dictionary<string, Severity>(StringComparer.Ordinal);
        try
        {
            foreach (var rules in XDocument.Load(file).Descendants("Rules").Where(r => (string?)r.Attribute("AnalyzerId") == "SonarAnalyzer.CSharp"))
            {
                foreach (var rule in rules.Elements("Rule"))
                {
                    if ((string?)rule.Attribute("Id") is { } id && Regex.IsMatch(id, @"^S\d+$") && StyleCopSetup.ParseSeverity((string?)rule.Attribute("Action") ?? string.Empty) is { } severity)
                    {
                        result[id] = severity;
                    }
                }
            }
        }
        catch (XmlException)
        {
            // Not a ruleset after all.
        }

        return result;
    }

    /// <summary>
    /// 'dotnet_diagnostic.Sxxxx.severity' in a .globalconfig, or in the .editorconfig sections for all C# files (later
    /// sections win).
    /// </summary>
    private static Dictionary<string, Severity> ReadConfig(string file, bool global)
    {
        var result = new Dictionary<string, Severity>(StringComparer.Ordinal);
        var inSection = global;
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
            if (line.StartsWith('['))
            {
                inSection = global || StyleCopSetup.AppliesToCSharp(line.Trim('[', ']'));
            }
            else if (inSection && KeyOf(line) is { } key && DiagnosticKey.Match(key) is { Success: true } match
                && StyleCopSetup.ParseSeverity(ValueOf(line).Split('#', ';')[0]) is { } severity)
            {
                result[match.Groups[1].Value.ToUpperInvariant()] = severity;
            }
        }

        return result;
    }

    private static IEnumerable<string[]> ReadTable(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} isn't embedded.");
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0 && !line.StartsWith('#'))
            {
                yield return line.Split('\t');
            }
        }
    }

    private static Dictionary<string, (bool, string)> LoadRules() =>
        ReadTable("SonarRules.tsv").ToDictionary(f => f[0], f => (f[1] == "on", f[3]), StringComparer.Ordinal);

    private static List<MappedRule> LoadMapping() =>
        ReadTable("SonarMapping.tsv").Select(f => new MappedRule(f[0], f[1], f[2] == "option", f[3] == "-" ? null : f[3], f[4] == "-" ? null : f[4])).ToList();

    /// <summary>A row of the mapping: a Sonar rule, the rule that fixes it, whether only its setting is written, and a note.</summary>
    internal sealed record MappedRule(string Sonar, string Rule, bool OptionOnly, string? Setting, string? Note);

    /// <summary>What <see cref="Apply"/> did, for the report.</summary>
    internal sealed class Applied
    {
        /// <summary>Gets or sets the number of Sonar rules on.</summary>
        public int On { get; set; }

        /// <summary>Gets the Sonar rules a StyleBro or SDK rule now fixes, with that rule (and its setting).</summary>
        public List<(string Sonar, string Target)> Enforced { get; } = new();

        /// <summary>Gets the Sonar rules the mapping knows but that aren't applied here, and why.</summary>
        public List<(string Sonar, string Reason)> NotApplied { get; } = new();

        /// <summary>Gets the Sonar rules on whose fixing rule isn't safe (<see cref="NotCovered"/>), and why.</summary>
        public List<(string Sonar, string Reason)> NotCovered { get; } = new();

        /// <summary>Gets settings that replace the StyleCop setup's.</summary>
        public List<string> Notes { get; } = new();
    }
}
