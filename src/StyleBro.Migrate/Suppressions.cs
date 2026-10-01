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
/// gets a sibling attribute for BRO1001. The StyleCop ones stay (harmless once StyleCop is gone, and needed while
/// both run). Running it again adds nothing.
/// </summary>
internal static class Suppressions
{
    private static readonly Regex CheckId = new(@"^(SA\d{4})\b");
    private static readonly Regex NoWarn = new(@"(<NoWarn(?:\s[^>]*)?>)([^<]*)(</NoWarn>)", RegexOptions.IgnoreCase);

    /// <summary>The rewritten text and the number of suppressions added (0: unchanged).</summary>
    public static (string Text, int Added) Rewrite(string text, IReadOnlyDictionary<string, SortedSet<string>> replacements)
    {
        var source = SourceText.From(text);
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var changes = new List<TextChange>();
        var added = 0;

        foreach (var pragma in root.DescendantTrivia(descendIntoTrivia: true)
            .Select(t => t.GetStructure()).OfType<PragmaWarningDirectiveTriviaSyntax>())
        {
            var codes = pragma.ErrorCodes.Select(c => c.ToString().Trim()).ToList();
            var missing = codes.Where(replacements.ContainsKey).SelectMany(c => replacements[c])
                .Distinct().Where(id => !codes.Contains(id)).ToList();
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
            foreach (var id in ids.Where(id => existing.Add(Key(attribute, id))))
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

    /// <summary>
    /// The same for MSBuild files: '&lt;NoWarn&gt;$(NoWarn);SA1123;SA1600&lt;/NoWarn&gt;' gets ';BRO1113;BRO1601'.
    /// </summary>
    public static (string Text, int Added) RewriteNoWarn(string text, IReadOnlyDictionary<string, SortedSet<string>> replacements)
    {
        var added = 0;
        var result = NoWarn.Replace(text, match =>
        {
            var value = match.Groups[2].Value;
            var codes = value.Split(';', ',').Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            var missing = codes.Where(replacements.ContainsKey).SelectMany(c => replacements[c])
                .Distinct().Where(id => !codes.Contains(id)).ToList();
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
