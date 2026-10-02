using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1508 (StyleCop SA1501: a statement's block isn't on a single line) and BRO1509 (SA1502: a type,
/// namespace, member body or accessor list isn't on a single line).
/// <para>
/// Like StyleCop: empty braces count; lambda and anonymous-method bodies don't; an accessor list only counts when an
/// accessor has a block body (<c>{ get; set; }</c> and <c>{ get => x; }</c> are fine), and single-line accessors inside a
/// multi-line property are fine. The fix rewrites only the whitespace gaps around the braces and between the items
/// inside them, so nested single-line blocks are separate, non-overlapping edits and single fix and Fix All agree:
/// every edit assumes that all enclosing single-line blocks are expanded too.
/// </para>
/// </summary>
internal static class SingleLineBlocks
{
    /// <summary>The nodes BRO1508/BRO1509 look at.</summary>
    public static readonly SyntaxKind[] Kinds =
    [
        SyntaxKind.Block,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.EnumDeclaration,
        SyntaxKind.NamespaceDeclaration,
        SyntaxKind.AccessorList,
    ];

    /// <summary>
    /// The braces of a node BRO1508/BRO1509 checks, and whether it's an element (BRO1509) rather than a statement block,
    /// or null. Method, constructor, operator and local function bodies are blocks reported as elements.
    /// </summary>
    public static (SyntaxToken Open, SyntaxToken Close, bool IsElement)? GetBraces(SyntaxNode node)
    {
        switch (node)
        {
            case BlockSyntax { Parent: AnonymousFunctionExpressionSyntax or AccessorDeclarationSyntax }:
                return null;
            case BlockSyntax block:
                return (block.OpenBraceToken, block.CloseBraceToken, block.Parent is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
            case BaseTypeDeclarationSyntax type when !type.OpenBraceToken.IsMissing && type.OpenBraceToken.RawKind != 0:
                return (type.OpenBraceToken, type.CloseBraceToken, true);
            case NamespaceDeclarationSyntax ns:
                return (ns.OpenBraceToken, ns.CloseBraceToken, true);
            case AccessorListSyntax list when list.Accessors.Any(a => a.Body is not null):
                return (list.OpenBraceToken, list.CloseBraceToken, true);
            default:
                return null;
        }
    }

    /// <summary>Whether the node is on a single line and the fix can expand it (BRO1508/BRO1509 report exactly these).</summary>
    public static bool IsReported(SyntaxNode node, SourceText text, AnalyzerConfigOptions options)
    {
        return GetChanges(node, text, options) is not null;
    }

    /// <summary>
    /// The edits that put the node on several lines, or null when it isn't on a single line or can't be expanded safely:
    /// a comment in one of the gaps the fix rewrites, or a node whose first line can't be determined (it shares its line
    /// with other code that stays, like <c>case 1: { ... }</c> after <c>switch (x) {</c> on the same line).
    /// </summary>
    /// <param name="trailingComma">Whether an expanded enum gets a trailing comma (BRO1401 is on; see <see cref="WantsTrailingComma"/>).</param>
    public static List<TextChange>? GetChanges(SyntaxNode node, SourceText text, AnalyzerConfigOptions options, bool trailingComma = true)
    {
        if (node.ContainsDiagnostics || GetBraces(node) is not { } braces || braces.Open.IsMissing || braces.Close.IsMissing
            || Line(text, braces.Open.SpanStart) != Line(text, braces.Close.SpanStart))
        {
            return null;
        }

        var unit = Indentation.GetUnit(options);
        if (StartIndent(Owner(node), text, options, unit) is not { } baseIndent)
        {
            return null;
        }

        var lineBreak = LineBreak(text, braces.Open.SpanStart);
        var inner = baseIndent + unit;
        var changes = new List<TextChange>();

        // Before '{': its own line, unless .editorconfig keeps braces on the line before (K&R style).
        var beforeOpen = braces.Open.GetPreviousToken();
        if (!Gap(beforeOpen, braces.Open, NewLineBeforeBrace(node, options) ? lineBreak + baseIndent : " ", text, changes))
        {
            return null;
        }

        // Inside: every item on its own line, one level deeper (also enum values: StyleCop's SA1502 fix keeps them on one
        // line, which its SA1136 then reports).
        var items = GetItems(node);
        if (items.Count == 0)
        {
            if (!Gap(braces.Open, braces.Close, lineBreak + baseIndent, text, changes))
            {
                return null;
            }
        }
        else
        {
            if (!Gap(braces.Open, items[0].GetFirstToken(), lineBreak + inner, text, changes))
            {
                return null;
            }

            // Members that BRO1505 wants separated get the blank line right away, so one 'dotnet format' run converges.
            for (var i = 1; i < items.Count; i++)
            {
                // Enum values: one per line (StyleCop's SA1136, BRO1121), no blank lines; the comma stays where it is.
                var separator = node is not (BlockSyntax or EnumDeclarationSyntax) && ElementSeparation.NeedsBlankLine(items[i - 1], items[i], text) ? lineBreak + lineBreak : lineBreak;
                if (!Gap(items[i].GetFirstToken().GetPreviousToken(), items[i].GetFirstToken(), separator + inner, text, changes))
                {
                    return null;
                }
            }

            // An expanded enum is a multi-line list, so BRO1401 wants a trailing comma: add it now, so the result doesn't
            // depend on whether 'dotnet format' runs BRO1401's fix before or after this one.
            var comma = node is EnumDeclarationSyntax @enum && @enum.Members.SeparatorCount < @enum.Members.Count && trailingComma ? "," : string.Empty;
            if (!Gap(items[items.Count - 1].GetLastToken(), braces.Close, comma + lineBreak + baseIndent, text, changes))
            {
                return null;
            }
        }

        // 'else', 'catch' and 'finally' on the line of the '}' before them get their own line (the SDK's
        // csharp_new_line_before_else/catch/finally, default true). A comment there: left as it is.
        var nextKeyword = braces.Close.GetNextToken();
        if (IsClauseKeyword(nextKeyword, options) && Line(text, nextKeyword.SpanStart) == Line(text, braces.Close.SpanStart))
        {
            Gap(braces.Close, nextKeyword, lineBreak + baseIndent, text, changes);
        }

        var beforeKeyword = Owner(node) is ElseClauseSyntax or CatchClauseSyntax or FinallyClauseSyntax ? Owner(node).GetFirstToken() : default;
        if (beforeKeyword.RawKind != 0 && IsClauseKeyword(beforeKeyword, options)
            && beforeKeyword.GetPreviousToken() is { RawKind: (int)SyntaxKind.CloseBraceToken } closeBefore
            && Line(text, closeBefore.SpanStart) == Line(text, beforeKeyword.SpanStart))
        {
            Gap(closeBefore, beforeKeyword, lineBreak + baseIndent, text, changes);
        }

        return changes;
    }

    /// <summary>
    /// What the braces belong to, for their indentation: the statement or clause that owns a block, the block itself when
    /// it stands alone, or the declaration.
    /// </summary>
    private static SyntaxNode Owner(SyntaxNode node) => node switch
    {
        BlockSyntax { Parent: BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax } block => block,
        BlockSyntax { Parent: { } parent } => parent,
        AccessorListSyntax { Parent: { } property } => property,
        _ => node,
    };

    /// <summary>
    /// The indentation of the line a node starts on once every enclosing single-line block is expanded, or null when
    /// that can't be told (the node shares its line with code that stays there).
    /// </summary>
    private static string? StartIndent(SyntaxNode node, SourceText text, AnalyzerConfigOptions options, string unit)
    {
        var first = node.GetFirstToken();
        var line = text.Lines.GetLineFromPosition(first.SpanStart);
        var before = text.ToString(TextSpan.FromBounds(line.Start, first.SpanStart));
        if (before.All(c => c is ' ' or '\t'))
        {
            return before;
        }

        switch (node)
        {
            case ElseClauseSyntax or CatchClauseSyntax or FinallyClauseSyntax:
                return node.Parent is { } statement ? StartIndent(statement, text, options, unit) : null;
            case IfStatementSyntax { Parent: ElseClauseSyntax elseClause }:
                return StartIndent(elseClause, text, options, unit);
        }

        // An item inside braces that get expanded starts its own line, one level deeper than those braces.
        SyntaxNode? container = node.Parent is BlockSyntax or BaseTypeDeclarationSyntax or NamespaceDeclarationSyntax or AccessorListSyntax
            ? node.Parent
            : null;
        if (container is not null && GetItems(container).Contains(node) && IsReported(container, text, options)
            && StartIndent(Owner(container), text, options, unit) is { } containerIndent)
        {
            return containerIndent + unit;
        }

        return null;
    }

    /// <summary>The items inside the braces, each of which gets its own line.</summary>
    private static IReadOnlyList<SyntaxNode> GetItems(SyntaxNode node) => node switch
    {
        BlockSyntax block => block.Statements,
        EnumDeclarationSyntax @enum => @enum.Members,
        TypeDeclarationSyntax type => type.Members,
        NamespaceDeclarationSyntax ns => ns.Externs.Cast<SyntaxNode>().Concat(ns.Usings).Concat(ns.Members).ToList(),
        AccessorListSyntax list => list.Accessors,
        _ => [],
    };

    /// <summary>
    /// Replaces the whitespace between two tokens with the given text (no edit when it's already that). False when the
    /// gap holds a comment or directive: the node is then skipped.
    /// </summary>
    internal static bool Gap(SyntaxToken before, SyntaxToken after, string newText, SourceText text, List<TextChange> changes)
    {
        // An enum's members are one item: its last token is the last member's, plus a trailing comma if there is one.
        if (after.Parent is EnumDeclarationSyntax @enum && after == @enum.CloseBraceToken && @enum.Members.Count > 0)
        {
            var last = @enum.Members.GetSeparators().LastOrDefault();
            before = @enum.Members.SeparatorCount == @enum.Members.Count && last.RawKind != 0 ? last : @enum.Members[@enum.Members.Count - 1].GetLastToken();
        }

        if (!before.TrailingTrivia.Concat(after.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return false;
        }

        var span = TextSpan.FromBounds(before.Span.End, after.SpanStart);
        if (text.ToString(span) != newText)
        {
            changes.Add(new TextChange(span, newText));
        }

        return true;
    }

    /// <summary>The SDK's csharp_new_line_before_open_brace for this kind of brace (default: all).</summary>
    internal static bool NewLineBeforeBrace(SyntaxNode node, AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("csharp_new_line_before_open_brace", out var value))
        {
            return true;
        }

        var kinds = value.Split(',').Select(v => v.Trim().ToLowerInvariant()).ToList();
        if (kinds.Contains("all"))
        {
            return true;
        }

        var kind = node switch
        {
            BlockSyntax { Parent: LocalFunctionStatementSyntax } => "local_functions",
            BlockSyntax { Parent: AccessorDeclarationSyntax } => "accessors",
            BlockSyntax { Parent: BaseMethodDeclarationSyntax } => "methods",
            BlockSyntax => "control_blocks",
            AccessorListSyntax { Parent: IndexerDeclarationSyntax } => "indexers",
            AccessorListSyntax { Parent: EventDeclarationSyntax } => "events",
            AccessorListSyntax => "properties",
            _ => "types",
        };
        return kinds.Contains(kind);
    }

    /// <summary>
    /// Whether BRO1401 (trailing comma in multi-line lists) is on for the file. Severities aren't in the analyzer
    /// options (the compiler takes the dotnet_diagnostic keys out), so they're read from the compilation's options.
    /// </summary>
    public static bool WantsTrailingComma(CompilationOptions? compilationOptions, SyntaxTree tree, CancellationToken cancellationToken) =>
        Severities.IsOn(compilationOptions, tree, DiagnosticIds.TrailingComma, cancellationToken);

    private static bool IsClauseKeyword(SyntaxToken token, AnalyzerConfigOptions options)
    {
        var option = token.Kind() switch
        {
            SyntaxKind.ElseKeyword => "csharp_new_line_before_else",
            SyntaxKind.CatchKeyword => "csharp_new_line_before_catch",
            SyntaxKind.FinallyKeyword => "csharp_new_line_before_finally",
            _ => null,
        };
        return option is not null
            && !(options.TryGetValue(option, out var value) && value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The line break the file uses: the one ending the node's line, or the first one in the file.</summary>
    internal static string LineBreak(SourceText text, int position)
    {
        var line = text.Lines.GetLineFromPosition(position);
        foreach (var candidate in new[] { line }.Concat(text.Lines))
        {
            if (candidate.EndIncludingLineBreak > candidate.End)
            {
                return text.ToString(TextSpan.FromBounds(candidate.End, candidate.EndIncludingLineBreak));
            }
        }

        return "\n";
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
