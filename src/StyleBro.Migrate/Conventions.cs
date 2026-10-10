using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;

namespace StyleBro.Migrate;

/// <summary>
/// The conventions a repository's code already follows, for the settings StyleBro has (owner's decision 2026-10-09,
/// docs/decisions.md: "Detect the main ones"). 'stylebro-migrate init' counts both forms of each in the repository's own
/// C# (syntax only; generated, vendored and EF Core migration files and submodules don't count) and writes the form most
/// of the code uses when it is clear: at least <see cref="Share"/> of at least <see cref="MinimumSample"/> places, or all of
/// at least <see cref="MinimumAgreeing"/>. Ocelot
/// writes '=>' at the start of a wrapped line; StyleBro's default (end) moved it in 115 files.
/// </summary>
internal static class Conventions
{
    /// <summary>The share of places one form needs before init writes it.</summary>
    public const double Share = 0.75;

    /// <summary>
    /// The fewest places a majority is judged on. Fewer places (but at least <see cref="MinimumAgreeing"/>) are followed only
    /// when they all agree; mixed ones turn the rule off like a missing majority.
    /// </summary>
    public const int MinimumSample = 10;

    /// <summary>
    /// The fewest places init judges at all (owner's decision 2026-10-10, docs/decisions.md): with fewer, the default stays and
    /// nothing is written. The package check's test project had one '""', and init switched BRO1106 to literals for it.
    /// </summary>
    public const int MinimumAgreeing = 3;

    /// <summary>
    /// Every convention init detects: the setting, what is counted, StyleBro's default (or the preset's), and the two values
    /// with what each looks like, and the line that stops StyleBro enforcing either form when the code has no clear majority
    /// (owner's decision 2026-10-10, docs/decisions.md; null: no StyleBro rule of its own enforces it, e.g. the SDK formatter's
    /// brace placement, so the default stays). <c>AlsoKeys</c> get the same value (else, catch and finally go together).
    /// </summary>
    public static readonly IReadOnlyList<Convention> All = new Convention[]
    {
        new(StyleBro.Analyzers.Naming.FieldNames.StyleKey, "private fields", "camelCase", ("camelCase", "named 'field'"), ("_camelCase", "named '_field'"), Off(DiagnosticIds.PrivateFieldNaming)),
        new("csharp_new_line_before_open_brace", "'{' of multi-line blocks", "all", ("all", "on its own line"), ("none", "at the end of the line"), null),
        new("csharp_new_line_before_else", "else/catch/finally after '}'", "true", ("true", "on a new line"), ("false", "on the '}' line"), null, "csharp_new_line_before_catch", "csharp_new_line_before_finally"),
        new("csharp_prefer_braces", "one-line if/else/for/while/using/lock bodies", "true", ("true", "with braces"), ("when_multiline", "without braces"), "csharp_prefer_braces = when_multiline"),
        new("dotnet_style_operator_placement_when_wrapping", "operators where a line wraps", "beginning_of_line", ("beginning_of_line", "at the start of the line"), ("end_of_line", "at the end of the line"), Off(DiagnosticIds.OperatorPlacement)),
        new(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, "'=>' where a line wraps", "end_of_line", ("end_of_line", "at the end of the line"), ("beginning_of_line", "at the start of the line"), Off(DiagnosticIds.ArrowPlacement)),
        new(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, "'=' where a line wraps", "end_of_line", ("end_of_line", "at the end of the line"), ("beginning_of_line", "at the start of the line"), Off(DiagnosticIds.EqualsPlacement)),
        new("stylebro_trailing_comma", "multi-line initializers", "include", ("include", "with a trailing comma"), ("omit", "without a trailing comma"), Off(DiagnosticIds.TrailingComma)),
        new("stylebro_empty_string_style", "empty strings", "string_empty", ("string_empty", "string.Empty"), ("literal", "\"\""), Off(DiagnosticIds.EmptyString)),
        new("stylebro_null_check_style", "null checks", "pattern_matching", ("pattern_matching", "'is null'"), ("equality_operator", "'== null'"), Off(DiagnosticIds.NullCheckStyle)),
        new("stylebro_summary_layout", "one-line <summary> texts", "multi_line", ("multi_line", "tags on lines of their own"), ("single_line_when_fits", "on one line"), Off(DiagnosticIds.SummaryLayout)),
        new("stylebro_inheritdoc_style", "<inheritdoc/> tags", "compact", ("compact", "'<inheritdoc/>'"), ("spaced", "'<inheritdoc />'"), null),
        new("csharp_prefer_simple_default_expression", "default values", "false", ("false", "'default(T)'"), ("true", "'default'"), null),
        new("stylebro_closing_parenthesis_placement", "')' of split lists", "last_item", ("last_item", "after the last item"), ("own_line", "on its own line"), Off(DiagnosticIds.CloseParenthesisOnLastItemLine)),
        new("stylebro_split_list_first_item", "split lists", "next_line", ("next_line", "first item on the next line"), ("same_line", "first item after '('"), Off(DiagnosticIds.SplitParametersStartOnNewLine)),
        new("stylebro_constructor_initializer_placement", "': base(...)'/': this(...)'", "own_line", ("own_line", "on its own line"), ("same_line", "on the constructor's line"), Off(DiagnosticIds.ConstructorInitializerLine)),
        new("stylebro_constraint_placement", "'where' constraints", "own_line", ("own_line", "on their own line"), ("same_line", "on the declaration's line"), Off(DiagnosticIds.ConstraintOnOwnLine)),
        new("stylebro_object_creation_parentheses", "'new T { ... }' with an initializer", "omit", ("omit", "without '()'"), ("include", "with '()'"), Off(DiagnosticIds.ObjectCreationParentheses)),
        new("stylebro_blank_line_between_switch_sections", "switch sections", "include", ("include", "after a blank line"), ("omit", "without a blank line"), Off(DiagnosticIds.BlankLineBetweenSwitchSections)),
        new("csharp_using_directive_placement", "files' using directives", "outside_namespace", ("outside_namespace", "outside the namespace"), ("inside_namespace", "inside the namespace"), Off(DiagnosticIds.UsingPlacement)),
        new(ArithmeticKey, "mixed arithmetic ('a + b * c')", "always_for_clarity", ("always_for_clarity", "with parentheses"), ("never_if_unnecessary", "without parentheses"), Off(DiagnosticIds.ArithmeticPrecedence)),
    };

    /// <summary>The SDK option BRO1406 follows ('never_if_unnecessary' turns it off).</summary>
    private const string ArithmeticKey = "dotnet_style_parentheses_in_arithmetic_binary_operators";

    /// <summary>Counts every convention in the repository's own C# files: key -> count per value (in <see cref="Convention.Values"/>' order).</summary>
    public static Dictionary<string, int[]> Count(string root)
    {
        var counts = NewCounts();
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            var text = File.ReadAllText(file);
            if (!Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))
            {
                Count(CSharpSyntaxTree.ParseText(text), counts);
            }
        }

        return counts;
    }

    /// <summary>An empty count for every convention.</summary>
    public static Dictionary<string, int[]> NewCounts() => All.ToDictionary(c => c.Key, _ => new int[2], StringComparer.Ordinal);

    /// <summary>Adds one file's places to <paramref name="counts"/>.</summary>
    public static void Count(SyntaxTree tree, Dictionary<string, int[]> counts)
    {
        var text = tree.GetText();
        int Line(int position) => text.Lines.GetLineFromPosition(position).LineNumber;
        bool StartsLine(SyntaxToken token)
        {
            var line = text.Lines.GetLineFromPosition(token.SpanStart);
            return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, token.SpanStart)));
        }

        static bool EndsLine(SyntaxToken token) => token.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        void Add(string key, bool second) => counts[key][second ? 1 : 0]++;

        // A token where a line wraps: before it (the second value, beginning_of_line) or after it (the first).
        void Wrap(string key, SyntaxToken token, bool beginningIsFirst = false)
        {
            var starts = StartsLine(token);
            var ends = EndsLine(token);
            if (starts != ends)
            {
                Add(key, starts != beginningIsFirst);
            }
        }

        var (underscore, plain) = Migration.CountPrivateFields(tree);
        counts[StyleBro.Analyzers.Naming.FieldNames.StyleKey][0] += plain;
        counts[StyleBro.Analyzers.Naming.FieldNames.StyleKey][1] += underscore;

        var root = tree.GetRoot();
        UsingPlacement();
        foreach (var token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent is BlockSyntax or BaseTypeDeclarationSyntax or BaseNamespaceDeclarationSyntax or AccessorListSyntax or SwitchStatementSyntax
                && token.Parent.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken)) is var close && Line(close.SpanStart) != Line(token.SpanStart)
                && token.GetPreviousToken() is { RawKind: not 0 } before && !before.IsKind(SyntaxKind.OpenBraceToken) && !before.IsKind(SyntaxKind.CloseBraceToken)
                && !before.IsKind(SyntaxKind.SemicolonToken) && !before.IsKind(SyntaxKind.ColonToken))
            {
                Add("csharp_new_line_before_open_brace", Line(before.SpanStart) == Line(token.SpanStart));
            }
            else if ((token.IsKind(SyntaxKind.ElseKeyword) || token.IsKind(SyntaxKind.CatchKeyword) || token.IsKind(SyntaxKind.FinallyKeyword))
                && token.GetPreviousToken().IsKind(SyntaxKind.CloseBraceToken))
            {
                Add("csharp_new_line_before_else", Line(token.GetPreviousToken().SpanStart) == Line(token.SpanStart));
            }
        }

        foreach (var node in root.DescendantNodes(descendIntoTrivia: true))
        {
            switch (node)
            {
                case IfStatementSyntax or ElseClauseSyntax or ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or UsingStatementSyntax or LockStatementSyntax:
                    var body = node switch
                    {
                        IfStatementSyntax s => s.Statement,
                        ElseClauseSyntax s => s.Statement,
                        ForStatementSyntax s => s.Statement,
                        CommonForEachStatementSyntax s => s.Statement,
                        WhileStatementSyntax s => s.Statement,
                        UsingStatementSyntax s => s.Statement,
                        _ => ((LockStatementSyntax)node).Statement,
                    };
                    var inner = body is BlockSyntax { Statements.Count: 1 } block ? block.Statements[0] : body;
                    if (inner is not (BlockSyntax or IfStatementSyntax or UsingStatementSyntax) && Line(inner.SpanStart) == Line(inner.Span.End))
                    {
                        Add("csharp_prefer_braces", body is not BlockSyntax);
                    }

                    break;

                case BinaryExpressionSyntax binary:
                    Wrap("dotnet_style_operator_placement_when_wrapping", binary.OperatorToken, beginningIsFirst: true);
                    if ((binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
                        && (binary.Left.IsKind(SyntaxKind.NullLiteralExpression) || binary.Right.IsKind(SyntaxKind.NullLiteralExpression)))
                    {
                        Add("stylebro_null_check_style", true);
                    }

                    Arithmetic(binary);
                    break;

                case ConditionalExpressionSyntax conditional:
                    Wrap("dotnet_style_operator_placement_when_wrapping", conditional.QuestionToken, beginningIsFirst: true);
                    Wrap("dotnet_style_operator_placement_when_wrapping", conditional.ColonToken, beginningIsFirst: true);
                    break;

                case ArrowExpressionClauseSyntax arrow:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, arrow.ArrowToken);
                    break;

                case SwitchExpressionArmSyntax arm:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, arm.EqualsGreaterThanToken);
                    break;

                case EqualsValueClauseSyntax equals:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, equals.EqualsToken);
                    break;

                case AssignmentExpressionSyntax assignment:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, assignment.OperatorToken);
                    break;

                case InitializerExpressionSyntax initializer when !initializer.IsKind(SyntaxKind.ComplexElementInitializerExpression):
                    TrailingComma(initializer.OpenBraceToken, initializer.CloseBraceToken, initializer.Expressions.Count, initializer.Expressions.SeparatorCount);
                    break;

                case AnonymousObjectCreationExpressionSyntax anonymous:
                    TrailingComma(anonymous.OpenBraceToken, anonymous.CloseBraceToken, anonymous.Initializers.Count, anonymous.Initializers.SeparatorCount);
                    break;

                case EnumDeclarationSyntax enumeration:
                    TrailingComma(enumeration.OpenBraceToken, enumeration.CloseBraceToken, enumeration.Members.Count, enumeration.Members.SeparatorCount);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    TrailingComma(switchExpression.OpenBraceToken, switchExpression.CloseBraceToken, switchExpression.Arms.Count, switchExpression.Arms.SeparatorCount);
                    break;

                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal when literal.Token.ValueText.Length == 0
                    && literal.Token.Text is "\"\"" or "@\"\"" && !InConstantContext(literal):
                    Add("stylebro_empty_string_style", true);
                    break;

                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Empty" } access
                    when access.Expression is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword } or IdentifierNameSyntax { Identifier.ValueText: "String" }:
                    Add("stylebro_empty_string_style", false);
                    break;

                case IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression } or UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression } } }:
                    Add("stylebro_null_check_style", false);
                    break;

                case DefaultExpressionSyntax:
                    Add("csharp_prefer_simple_default_expression", false);
                    break;

                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression }:
                    Add("csharp_prefer_simple_default_expression", true);
                    break;

                case ArgumentListSyntax or ParameterListSyntax:
                    var (open, items, closeToken) = node is ArgumentListSyntax a
                        ? (a.OpenParenToken, a.Arguments.Cast<SyntaxNode>().ToList(), a.CloseParenToken)
                        : (((ParameterListSyntax)node).OpenParenToken, ((ParameterListSyntax)node).Parameters.Cast<SyntaxNode>().ToList(), ((ParameterListSyntax)node).CloseParenToken);
                    if (items.Count > 0 && !closeToken.IsMissing && Line(open.SpanStart) != Line(closeToken.SpanStart))
                    {
                        if (StartsLine(closeToken))
                        {
                            Add("stylebro_closing_parenthesis_placement", true);
                        }
                        else if (Line(items[^1].Span.End) == Line(closeToken.SpanStart) && Line(items[^1].SpanStart) != Line(open.SpanStart))
                        {
                            Add("stylebro_closing_parenthesis_placement", false);
                        }

                        if (items.Count > 1 && Line(items[0].SpanStart) != Line(items[1].SpanStart))
                        {
                            Add("stylebro_split_list_first_item", Line(items[0].SpanStart) == Line(open.SpanStart));
                        }
                    }

                    break;

                case ConstructorInitializerSyntax initializer:
                    Add("stylebro_constructor_initializer_placement", !StartsLine(initializer.ColonToken));
                    break;

                case TypeParameterConstraintClauseSyntax constraint:
                    Add("stylebro_constraint_placement", !StartsLine(constraint.WhereKeyword));
                    break;

                case ObjectCreationExpressionSyntax { Initializer: not null } creation:
                    Add("stylebro_object_creation_parentheses", creation.ArgumentList is not null);
                    break;

                case SwitchStatementSyntax switchStatement:
                    foreach (var section in switchStatement.Sections.Skip(1))
                    {
                        var line = Line(section.SpanStart);
                        Add("stylebro_blank_line_between_switch_sections", line < 1 || !string.IsNullOrWhiteSpace(text.Lines[line - 1].ToString()));
                    }

                    break;

                case XmlElementSyntax { StartTag.Name.LocalName.ValueText: "summary" } summary:
                    var textLines = summary.Content.SelectMany(c => c.DescendantTokens())
                        .Where(t => !t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && !string.IsNullOrWhiteSpace(t.Text))
                        .Select(t => Line(t.SpanStart))
                        .Distinct()
                        .ToList();
                    var startLine = Line(summary.StartTag.SpanStart);
                    var endLine = Line(summary.EndTag.SpanStart);
                    if (textLines.Count == 1 && !summary.EndTag.IsMissing)
                    {
                        if (startLine == endLine)
                        {
                            Add("stylebro_summary_layout", true);
                        }
                        else if (textLines[0] != startLine && textLines[0] != endLine)
                        {
                            Add("stylebro_summary_layout", false);
                        }
                    }

                    break;

                case XmlEmptyElementSyntax { Name.LocalName.ValueText: "inheritdoc" } inheritdoc:
                    Add("stylebro_inheritdoc_style", inheritdoc.SlashGreaterThanToken.GetPreviousToken().TrailingTrivia.Any() || inheritdoc.SlashGreaterThanToken.LeadingTrivia.Any());
                    break;
            }
        }

        // Unit = files: one with a namespace and non-global usings on one level only (file-scoped namespaces count as inside).
        void UsingPlacement()
        {
            if (root is CompilationUnitSyntax unit && unit.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>().ToList() is { Count: > 0 } namespaces)
            {
                var outside = unit.Usings.Any(u => u.GlobalKeyword.IsKind(SyntaxKind.None));
                var inside = namespaces.Any(n => n.Usings.Count > 0);
                if (outside != inside)
                {
                    Add("csharp_using_directive_placement", inside);
                }
            }
        }

        // BRO1406's places: an operation of another kind inside arithmetic. Without parentheses it is the rule's finding; with
        // them only where they change nothing ('a + (b * c)', not '(a + b) * c').
        void Arithmetic(BinaryExpressionSyntax binary)
        {
            var outer = Level(binary.Kind());
            foreach (var operand in outer == 0 ? Array.Empty<ExpressionSyntax>() : new[] { binary.Left, binary.Right })
            {
                var parenthesized = operand is ParenthesizedExpressionSyntax;
                if ((operand is ParenthesizedExpressionSyntax p ? p.Expression : operand) is BinaryExpressionSyntax inner && Level(inner.Kind()) is var level and > 0
                    && Family(inner.Kind()) != Family(binary.Kind()) && (!parenthesized || level > outer || (level == outer && operand == binary.Left)))
                {
                    Add(ArithmeticKey, !parenthesized);
                }
            }
        }

        static int Level(SyntaxKind kind) => kind switch
        {
            SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression or SyntaxKind.ModuloExpression => 3,
            SyntaxKind.AddExpression or SyntaxKind.SubtractExpression => 2,
            SyntaxKind.LeftShiftExpression or SyntaxKind.RightShiftExpression => 1,
            _ => 0,
        };

        // StyleCop's families: '%' is one of its own.
        static int Family(SyntaxKind kind) => kind == SyntaxKind.ModuloExpression ? 4 : Level(kind);

        void TrailingComma(SyntaxToken open, SyntaxToken close, int items, int separators)
        {
            if (items > 0 && Line(open.SpanStart) != Line(close.SpanStart))
            {
                Add("stylebro_trailing_comma", separators < items);
            }
        }
    }

    /// <summary>
    /// What to write: for each convention with a clear majority (<see cref="Share"/> of at least <see cref="MinimumSample"/>
    /// places, or at least <see cref="MinimumAgreeing"/> places that all agree) that isn't the default, its lines; without one,
    /// the line that turns its rule off (<see cref="Convention.Off"/>; owner's decision 2026-10-10: Bogus had 24 operators at
    /// the start of a line and 50 at the end, and BRO1520 moved 52); and the report: one line per convention found in the code
    /// (the verdict first) and a summary of what was kept and turned off. <paramref name="decided"/>: keys set elsewhere (the
    /// repository's own .editorconfig, the Sonar setup, an SDK naming rule), with why; they're reported, not written.
    /// <paramref name="isSet"/>: whether the repository or the Sonar setup sets an Off line's key itself (then it stays).
    /// </summary>
    public static (List<string> Lines, List<string> Report) Decide(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> decided, Func<string, bool>? isSet = null)
    {
        var lines = new List<string>();
        var report = new List<string>();
        var kept = new List<string>();
        var turnedOff = new List<string>();
        var tooFew = 0;
        foreach (var convention in All)
        {
            var count = counts.TryGetValue(convention.Key, out var c) ? c : new int[2];
            var total = count[0] + count[1];
            if (total == 0)
            {
                continue;
            }

            var mixed = string.Join(", ", Enumerable.Range(0, 2).Where(i => count[i] > 0).Select(i => $"{count[i]} {convention.Values[i].Label}"));
            var winner = total < MinimumSample ? (count[0] == 0 ? 1 : count[1] == 0 ? 0 : -1)
                : count[1] >= Share * total ? 1 : count[0] >= Share * total ? 0 : -1;
            var found = winner < 0 ? mixed : $"{count[winner]} of {total} {convention.Values[winner].Label}";
            string tag;
            string verdict;
            if (decided.TryGetValue(convention.Key, out var reason))
            {
                (tag, verdict) = ("set", reason);
            }
            else if (total < MinimumAgreeing)
            {
                (tag, verdict) = ("too few", $"too few places to tell, {convention.Key} stays {convention.Default}");
                found = mixed;
                tooFew++;
            }
            else if (winner < 0 && convention.Off is { } off && !(isSet?.Invoke(off.Substring(0, off.IndexOf('=')).Trim()) ?? false))
            {
                var rule = off.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) ? off.Split('.')[1] : null;
                (tag, verdict) = ("off", rule is null ? $"both forms allowed: {off}" : $"{rule} is off");
                turnedOff.Add($"{rule ?? off} ({convention.What})");
                lines.Add(rule is null
                    ? $"# init: {convention.What}: your code mixes both forms ({mixed}), so both are allowed. To choose {convention.Values[0].Label}: change the next line to {convention.Key} = {convention.Values[0].Value}."
                    : $"# init: {convention.What}: your code mixes both forms ({mixed}), so {rule} is off. To choose one: set {convention.Key} and remove the next line.");
                lines.Add(off);
            }
            else if (winner < 0)
            {
                (tag, verdict) = ("mixed", $"{convention.Key} stays {convention.Default}");
            }
            else if (convention.Values[winner].Value == convention.Default)
            {
                (tag, verdict) = ("default", $"{convention.Key} = {convention.Default}");
            }
            else
            {
                var value = convention.Values[winner].Value;
                (tag, verdict) = ("kept", $"{convention.Key} = {value}");
                kept.Add(convention.Key);
                lines.Add($"# init: {convention.What}: {convention.Values[winner].Label} in {count[winner]} of {total} places in your code");
                lines.AddRange(new[] { convention.Key }.Concat(convention.AlsoKeys).Select(k => $"{k} = {value}"));
            }

            report.Add($"  {tag,-8} {convention.What}: {found} -> {verdict}");
        }

        report.AddRange(Summary(kept, turnedOff, tooFew));
        return (lines, report);
    }

    /// <summary>The end of init's convention report: which settings follow the code, which rules are off and how to choose later.</summary>
    public static IEnumerable<string> Summary(IReadOnlyList<string> kept, IReadOnlyList<string> turnedOff, int tooFew)
    {
        if (kept.Count + turnedOff.Count + tooFew == 0)
        {
            yield break;
        }

        yield return "Summary:";
        if (kept.Count > 0)
        {
            yield return $"  Kept your style for {kept.Count} setting(s): {string.Join(", ", kept)}.";
        }

        if (turnedOff.Count > 0)
        {
            yield return $"  Turned {turnedOff.Count} rule(s) off because your code mixes both forms: {string.Join(", ", turnedOff)}.";
            yield return "  To choose a style later: set the key in .editorconfig, remove its 'severity = none' line, run 'stylebro-migrate format'.";
        }

        if (tooFew > 0)
        {
            yield return $"  {tooFew} setting(s) had fewer than {MinimumAgreeing} places to tell: StyleBro's defaults stay.";
        }
    }

    /// <summary>The line that turns a rule off.</summary>
    private static string Off(string id) => $"dotnet_diagnostic.{id}.severity = none";

    /// <summary>Whether an empty string literal has to stay a literal (BRO1106 leaves constant contexts alone).</summary>
    private static bool InConstantContext(SyntaxNode literal)
    {
        return literal.Ancestors().Any(a => a is AttributeArgumentSyntax or ParameterSyntax or CaseSwitchLabelSyntax or PatternSyntax
            || (a is FieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.ConstKeyword))
            || (a is LocalDeclarationStatementSyntax local && local.IsConst));
    }

    /// <summary>A convention init detects (see <see cref="All"/>).</summary>
    internal sealed class Convention
    {
        public Convention(string key, string what, string defaultValue, (string Value, string Label) first, (string Value, string Label) second, string? off, params string[] alsoKeys)
        {
            Key = key;
            What = what;
            Default = defaultValue;
            Values = new[] { first, second };
            Off = off;
            AlsoKeys = alsoKeys;
        }

        public string Key { get; }

        public string What { get; }

        public string Default { get; }

        public (string Value, string Label)[] Values { get; }

        /// <summary>Gets the line written when the code has no clear majority (see <see cref="Conventions.All"/>), or null.</summary>
        public string? Off { get; }

        public string[] AlsoKeys { get; }
    }
}
