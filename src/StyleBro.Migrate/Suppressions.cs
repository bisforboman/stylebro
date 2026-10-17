using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Migrate;

/// <summary>
/// Carries StyleCop suppressions over to the rules that replace them, so code a team deliberately exempted stays
/// exempt: '#pragma warning disable SA1642' gets ', BRO1606', and '[SuppressMessage("StyleCop...", "SA1202:...")]'
/// gets a sibling attribute for BRO1001. Sonar's suppressions the same way ('S2325' gets 'CA1822', <see cref="WithSonar"/>).
/// The StyleCop and Sonar ones stay (harmless once StyleCop is gone, and needed while both run). Running it again adds
/// nothing.
/// </summary>
internal static class Suppressions
{
    /// <summary>Rules whose suppressions do nothing under 'dotnet format': its whitespace pass formats regardless (IDE0055).</summary>
    public static readonly IReadOnlySet<string> Ineffective = new HashSet<string>(StringComparer.Ordinal) { "IDE0055" };

    /// <summary>StyleBro's own value of each option a Sonar 'option' row sets (sonar-mapping.tsv): what the code gets where the Sonar rule is suppressed.</summary>
    public static readonly IReadOnlyDictionary<string, string> OptionDefaults = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["stylebro_keep_overloads_together"] = "false",
    };

    private static readonly Regex CheckId = new(@"^(SA\d{4}|SX\d{4}|SST\d{4}|S\d{3,4})\b", RegexOptions.IgnoreCase);

    private static readonly Regex NoWarn = new(@"(<NoWarn(?:\s[^>]*)?>)([^<]*)(</NoWarn>)", RegexOptions.IgnoreCase);

    /// <summary>
    /// <paramref name="replacements"/> (StyleCop's) plus StyleSharp's: each SST rule gets the rules that replace the StyleCop
    /// rules it stands for (<see cref="StyleCopSetup.StandsFor"/>), so '#pragma warning disable SST1309' keeps BRO1303 out too.
    /// </summary>
    public static SortedDictionary<string, SortedSet<string>> WithStyleSharp(IReadOnlyDictionary<string, SortedSet<string>> replacements, StyleCopSetup setup)
    {
        var all = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (id, ids) in replacements)
        {
            all[id] = new SortedSet<string>(ids, StringComparer.Ordinal);
        }

        foreach (var (sst, _, _, _, styleCop) in setup.StyleSharp is null ? [] : StyleCopSetup.StyleSharpRules)
        {
            var ids = StyleCopSetup.StandsFor(sst, styleCop, setup.StyleSharp!).Where(replacements.ContainsKey).SelectMany(id => replacements[id]).ToList();
            if (ids.Count > 0)
            {
                all[sst] = new SortedSet<string>(ids, StringComparer.Ordinal);
            }
        }

        return all;
    }

    /// <summary>
    /// <paramref name="replacements"/> without the rules the generated settings (<paramref name="lines"/>) only turn off: a
    /// suppression of a rule that never reports is noise, and StyleSharp's SST1462 reports it (Splat: '[SuppressMessage("StyleBro",
    /// "BRO1311")]' next to an SA1316 suppression, with BRO1311 off).
    /// </summary>
    public static SortedDictionary<string, SortedSet<string>> WithoutOff(IReadOnlyDictionary<string, SortedSet<string>> replacements, IEnumerable<string> lines)
    {
        var severities = lines.Select(l => Regex.Match(l, @"^dotnet_diagnostic\.(\w+)\.severity\s*=\s*(\w+)")).Where(m => m.Success).ToList();
        var off = severities.Where(m => m.Groups[2].Value == "none").Select(m => m.Groups[1].Value)
            .Except(severities.Where(m => m.Groups[2].Value != "none").Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);
        return new SortedDictionary<string, SortedSet<string>>(
            replacements.ToDictionary(r => r.Key, r => new SortedSet<string>(r.Value.Where(id => !off.Contains(id)), StringComparer.Ordinal)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// <paramref name="replacements"/> (StyleCop's) plus Sonar's: each Sonar rule the mapping knows, to the rules that fix
    /// what it reports (a team that suppressed S2325 doesn't want CA1822's fix there either). With <paramref name="sonar"/>,
    /// only the Sonar rules that are on: an off one turns nothing on, so its suppressions need no replacing id (linkdotnet/Blog
    /// had S8969 off and in NoWarn, and BRO1147 was added next to it).
    /// </summary>
    public static SortedDictionary<string, SortedSet<string>> WithSonar(IReadOnlyDictionary<string, SortedSet<string>> replacements, SonarSetup? sonar = null)
    {
        var all = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (id, ids) in replacements)
        {
            all[id] = new SortedSet<string>(ids, StringComparer.Ordinal);
        }

        // An 'option' row only sets an option of a rule that's on anyway: suppressing the Sonar rule turns the option back
        // there (<see cref="OptionScopes"/>), not the whole rule off (bcrypt.net: S4136 in NoWarn suppressed all of BRO1001).
        foreach (var row in SonarSetup.Mapping.Where(m => !m.OptionOnly && (sonar?.IsOn(m.Sonar) ?? true)))
        {
            if (!all.TryGetValue(row.Sonar, out var ids))
            {
                all[row.Sonar] = ids = new SortedSet<string>(StringComparer.Ordinal);
            }

            ids.Add(row.Rule);
        }

        return all;
    }

    /// <summary>
    /// The sections for the folders whose MSBuild files suppress a Sonar 'option' row's rule in &lt;NoWarn&gt; (owner's choice
    /// 2026-10-14): the options it set in <paramref name="lines"/> go back to StyleBro's own value there, and the rule stays on
    /// (bcrypt.net's src/Directory.Build.props has S4136: BRO1001 sorts there, without keeping overloads together).
    /// </summary>
    public static List<(string Section, List<string> Lines)> OptionScopes(string root, SonarSetup sonar, IReadOnlyList<string> lines)
    {
        var result = new List<(string Section, List<string> Lines)>();
        var rows = SonarSetup.Mapping.Where(m => m.OptionOnly && m.Setting is not null && sonar.IsOn(m.Sonar)).ToList();
        if (rows.Count == 0)
        {
            return result;
        }

        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).OrderBy(f => f, StringComparer.Ordinal))
        {
            var codes = NoWarn.Matches(Migration.ReadOrNull(file) ?? string.Empty).SelectMany(m => m.Groups[2].Value.Split(';', ',')).Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var reset = rows.Where(r => codes.Contains(r.Sonar))
                .SelectMany(r => r.Setting!.Split("; ").Select(setting => (r.Sonar, Key: setting.Split('=')[0].Trim())))
                .Where(p => OptionDefaults.ContainsKey(p.Key) && lines.Any(l => l.Split('=')[0].Trim() == p.Key))
                .ToList();
            if (reset.Count == 0)
            {
                continue;
            }

            var relative = System.IO.Path.GetRelativePath(root, file).Replace('\\', '/');
            var folder = System.IO.Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
            result.Add((folder.Length == 0 ? "*.cs" : StyleCopSetup.SectionFor([folder]),
                [$"# {string.Join(", ", reset.Select(p => p.Sonar).Distinct())} in {relative}'s <NoWarn>: StyleBro's own value there, the rule stays on",
                    .. reset.Select(p => $"{p.Key} = {OptionDefaults[p.Key]}").Distinct()]));
        }

        return result;
    }

    /// <summary>
    /// <paramref name="report"/> (<see cref="SonarSetup.Report"/>) with the Sonar rules <paramref name="scopes"/> (<see cref="OptionScopes"/>)
    /// reset: everywhere (a root MSBuild file's NoWarn) the option isn't applied at all, else it isn't in those folders (trial 20:
    /// the report said "S4136 -> BRO1001 (stylebro_keep_overloads_together = same_access ...)" while the block wrote false).
    /// </summary>
    public static List<string> Annotate(List<string> report, IReadOnlyList<(string Section, List<string> Lines)> scopes)
    {
        foreach (var (section, lines) in scopes)
        {
            if (lines.Count < 2 || Regex.Match(lines[0], "^# (.+?) in (.+)'s <NoWarn>:") is not { Success: true } match)
            {
                continue;
            }

            var settings = string.Join(", ", lines.Skip(1));
            foreach (var id in match.Groups[1].Value.Split(", "))
            {
                var at = report.FindIndex(l => l.StartsWith($"  {id} -> ", StringComparison.Ordinal));
                if (at >= 0)
                {
                    var target = report[at].Substring($"  {id} -> ".Length).Split(' ')[0];
                    report[at] = section == "*.cs"
                        ? $"  {id} -> {target} keeps {settings} (StyleBro's own value): {match.Groups[2].Value}'s <NoWarn> has {id}"
                        : $"{report[at]}; {settings} (StyleBro's own value) in {section}: {match.Groups[2].Value}'s <NoWarn> has {id}";
                }
            }
        }

        return report;
    }

    /// <summary>The rewritten text and the number of suppressions added (0: unchanged).</summary>
    public static (string Text, int Added) Rewrite(string text, IReadOnlyDictionary<string, SortedSet<string>> replacements)
    {
        var source = SourceText.From(text);
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var changes = new List<TextChange>();
        var added = 0;

        foreach (var pragma in root.DescendantTrivia(descendIntoTrivia: true)
            .Select(t => t.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>())
        {
            var codes = pragma.ErrorCodes.Select(c => c.ToString().Trim()).ToList();
            var missing = codes.Where(replacements.ContainsKey).SelectMany(c => replacements[c])
                .Distinct()
                .Where(id => !codes.Contains(id))
                .ToList();
            if (missing.Count > 0)
            {
                changes.Add(new TextChange(new TextSpan(pragma.ErrorCodes.Last().Span.End, 0), ", " + string.Join(", ", missing)));
                added += missing.Count;
            }
        }

        var attributes = root.DescendantNodes().OfType<AttributeSyntax>().Where(IsSuppressMessage).ToList();
        var existing = attributes.Select(a => Key(a, GetCheckId(a) ?? string.Empty)).ToHashSet();
        var newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        foreach (var attribute in attributes)
        {
            if (GetCheckId(attribute) is not { } checkId || CheckId.Match(checkId) is not { Success: true } match
                || !replacements.TryGetValue(match.Groups[1].Value, out var ids))
            {
                continue;
            }

            var list = (AttributeListSyntax)attribute.Parent!;
            var rest = attribute.ArgumentList!.Arguments.Skip(2).Select(a => a.ToString());

            // On a type or member, SA1300's namespace part can't apply: no BRO1312 there (trial 23: noise StyleSharp reports).
            foreach (var id in ids.Where(id => !(id == StyleBro.Analyzers.DiagnosticIds.NamespacePascalCase && list.Parent is MemberDeclarationSyntax)).Where(id => existing.Add(Key(attribute, id))))
            {
                var arguments = new[] { $"\"{(id.StartsWith("BRO", StringComparison.Ordinal) ? "StyleBro" : "Style")}\"", $"\"{id}\"" }.Concat(rest);
                var copy = $"[{(list.Target is null ? string.Empty : list.Target + " ")}{attribute.Name}({string.Join(", ", arguments)})]";
                var nextToken = list.GetLastToken().GetNextToken();
                var line = source.Lines.GetLineFromPosition(list.SpanStart);
                var sameLine = nextToken.RawKind != 0 && source.Lines.GetLineFromPosition(nextToken.SpanStart).LineNumber == line.LineNumber;
                var indentation = source.ToString(TextSpan.FromBounds(line.Start, list.SpanStart));
                var separator = sameLine || indentation.Trim().Length > 0 ? " " : newLine + indentation;
                changes.Add(new TextChange(new TextSpan(list.Span.End, 0), separator + copy));
                added++;
            }
        }

        return changes.Count == 0 ? (text, 0) : (source.WithChanges(changes).ToString(), added);
    }

    /// <summary>The same for MSBuild files: '&lt;NoWarn&gt;$(NoWarn);SA1123;SA1600&lt;/NoWarn&gt;' gets ';BRO1113;BRO1601'.</summary>
    public static (string Text, int Added) RewriteNoWarn(string text, IReadOnlyDictionary<string, SortedSet<string>> replacements)
    {
        var added = 0;
        var result = NoWarn.Replace(text, match =>
        {
            var value = match.Groups[2].Value;
            var codes = value.Split(';', ',').Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            var missing = codes.Where(replacements.ContainsKey).SelectMany(c => replacements[c])
                .Distinct()
                .Where(id => !codes.Contains(id))
                .ToList();
            if (missing.Count == 0)
            {
                return match.Value;
            }

            added += missing.Count;
            return match.Groups[1].Value + value.TrimEnd() + ";" + string.Join(";", missing) + match.Groups[3].Value;
        });
        return (result, added);
    }

    private static bool IsSuppressMessage(AttributeSyntax attribute)
    {
        var name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => string.Empty,
        };
        return name is "SuppressMessage" or "SuppressMessageAttribute" && attribute.ArgumentList?.Arguments.Count >= 2;
    }

    private static string? GetCheckId(AttributeSyntax attribute)
    {
        return attribute.ArgumentList!.Arguments[1].Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
    }

    /// <summary>What a suppression applies to: the declaration (or the file, for assembly-level ones), its rule, and its scope/target arguments.</summary>
    private static string Key(AttributeSyntax attribute, string checkId)
    {
        var list = (AttributeListSyntax)attribute.Parent!;
        var id = CheckId.Match(checkId) is { Success: true } sa ? sa.Groups[1].Value : checkId.Split(':')[0].Trim();
        var rest = string.Join(",", attribute.ArgumentList!.Arguments.Skip(2)
            .Where(a => a.NameEquals?.Name.Identifier.ValueText is not "Justification")
            .Select(a => Regex.Replace(a.ToString(), @"\s+", string.Empty)));
        return $"{list.Parent!.SpanStart}|{list.Target}|{id}|{rest}";
    }
}
