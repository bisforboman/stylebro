using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Readability;

namespace StyleBro.Analyzers.Ordering;

/// <summary>Where using directives go: the SDK's csharp_using_directive_placement.</summary>
internal enum UsingPlacementMode
{
    /// <summary>'inside_namespace', or not set (StyleCop's default).</summary>
    Inside,

    /// <summary>'outside_namespace'.</summary>
    Outside,

    /// <summary>Any other value: nothing is checked.</summary>
    Preserve,
}

/// <summary>
/// Shared logic for BRO1008 (StyleCop SA1200): using directives inside or outside the namespace. The usings of a file
/// move together, and only when every name in the file binds to the same symbol afterwards: moving a using changes
/// where lookup finds it (the SDK's IDE0065 fix made 'Order' ambiguous in eShop and turned a type into a namespace in
/// Ocelot).
/// </summary>
internal static class UsingPlacement
{
    public const string ConfigKey = "csharp_using_directive_placement";

    private static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface)
        .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

    public static UsingPlacementMode GetMode(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(ConfigKey, out var value))
        {
            return UsingPlacementMode.Inside;
        }

        return value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "inside_namespace" => UsingPlacementMode.Inside,
            "outside_namespace" => UsingPlacementMode.Outside,
            _ => UsingPlacementMode.Preserve,
        };
    }

    /// <summary>The usings that are in the wrong place: the cheap syntax check before <see cref="GetChanges"/>.</summary>
    public static ImmutableArray<UsingDirectiveSyntax> GetMisplaced(CompilationUnitSyntax root, UsingPlacementMode mode) =>
        mode switch
        {
            UsingPlacementMode.Outside => root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .OfType<BaseNamespaceDeclarationSyntax>().SelectMany(n => n.Usings).ToImmutableArray(),
            UsingPlacementMode.Inside when root.Members.Any(m => m is BaseNamespaceDeclarationSyntax) =>
                root.Usings.Where(u => !u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)).ToImmutableArray(),
            _ => ImmutableArray<UsingDirectiveSyntax>.Empty,
        };

    /// <summary>
    /// The edits that move every using of the file to its place, or null when that can't be done safely: not exactly
    /// one namespace and nothing else in the file, usings on both levels, global usings or extern aliases, a directive
    /// before the namespace's body, a using sharing its line with other code, syntax errors, or a name that would bind
    /// differently afterwards. Comments directly above the usings move with them (not the file header).
    /// </summary>
    public static List<TextChange>? GetChanges(SemanticModel model, UsingPlacementMode mode, string indentUnit, CancellationToken cancellationToken)
    {
        var tree = model.SyntaxTree;
        var text = tree.GetText(cancellationToken);
        if (tree.GetRoot(cancellationToken) is not CompilationUnitSyntax root
            || GetSyntaxChanges(root, text, mode, indentUnit, model, cancellationToken) is not { } changes)
        {
            return null;
        }

        return BindsTheSame(model, root, text.WithChanges(changes), mode, cancellationToken) ? changes : null;
    }

    private static List<TextChange>? GetSyntaxChanges(
        CompilationUnitSyntax root,
        SourceText text,
        UsingPlacementMode mode,
        string indentUnit,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        if (mode == UsingPlacementMode.Preserve || root.ContainsDiagnostics || root.Externs.Count > 0 || root.AttributeLists.Count > 0
            || root.Members.Count != 1 || root.Members[0] is not BaseNamespaceDeclarationSyntax ns
            || ns.Externs.Count > 0 || ns.Members.Any(m => m is BaseNamespaceDeclarationSyntax))
        {
            return null;
        }

        // Usings on both levels, and global usings (which can't go inside), fail the binding check (BindsTheSame).
        var moved = mode == UsingPlacementMode.Outside ? ns.Usings : root.Usings;
        if (moved.Count == 0)
        {
            return null;
        }

        // Where the namespace's body starts: no directive ('#if', '#pragma', ...) may sit anywhere before it.
        var bodyStart = ns is NamespaceDeclarationSyntax blockNamespace ? blockNamespace.OpenBraceToken : ((FileScopedNamespaceDeclarationSyntax)ns).SemicolonToken;
        var checkedEnd = mode == UsingPlacementMode.Outside ? moved.Last().FullSpan.End : bodyStart.FullSpan.End;
        if (root.DescendantTrivia(TextSpan.FromBounds(0, checkedEnd)).Any(t => t.IsDirective))
        {
            return null;
        }

        var lines = text.Lines;
        var first = moved.First();
        var last = moved.Last();
        var firstLine = lines.GetLineFromPosition(first.SpanStart).LineNumber;
        var lastLine = lines.GetLineFromPosition(last.Span.End).LineNumber;
        var indentation = Indentation(lines[firstLine]);
        if (lines[firstLine].Start + indentation.Length != first.SpanStart
            || lines.GetLineFromPosition(last.GetLastToken().GetNextToken().SpanStart).LineNumber == lastLine)
        {
            return null;
        }

        // Comment lines directly above the first using move along, unless they start the file (its header).
        var startLine = firstLine;
        while (startLine > 0 && !IsBlank(lines[startLine - 1]) && lines[startLine - 1].Start >= first.FullSpan.Start)
        {
            startLine--;
        }

        if (startLine == 0 && mode == UsingPlacementMode.Inside)
        {
            startLine = firstLine;
        }

        // The moved lines, with relative names qualified (BRO1126's fix, so both rules agree in any order) and their
        // indentation replaced.
        var block = text.GetSubText(TextSpan.FromBounds(lines[startLine].Start, lines[lastLine].EndIncludingLineBreak));
        if (mode == UsingPlacementMode.Outside)
        {
            var qualified = moved
                .Select(u => (Name: u.Name, Qualified: QualifiedUsings.GetQualifiedName(u, model, cancellationToken)))
                .Where(p => p.Name is not null && p.Qualified is not null)
                .Select(p => new TextChange(new TextSpan(p.Name!.SpanStart - lines[startLine].Start, p.Name.Span.Length), p.Qualified!));
            block = block.WithChanges(qualified);
        }

        var newIndentation = mode == UsingPlacementMode.Outside ? string.Empty
            : ns is NamespaceDeclarationSyntax ? Indentation(lines.GetLineFromPosition(ns.SpanStart)) + indentUnit
            : string.Empty;
        var reindented = new System.Text.StringBuilder();
        foreach (var line in block.Lines)
        {
            var content = block.ToString(line.SpanIncludingLineBreak);
            if (!IsBlank(line))
            {
                if (!content.StartsWith(indentation, System.StringComparison.Ordinal))
                {
                    return null;
                }

                content = newIndentation + content.Substring(indentation.Length);
            }

            reindented.Append(content);
        }

        // The removal takes the blank lines below; one stays where code above would otherwise touch what follows.
        var endLine = lastLine + 1;
        while (endLine < lines.Count && IsBlank(lines[endLine]))
        {
            endLine++;
        }

        var aboveIsOpenBrace = ns is NamespaceDeclarationSyntax braces && startLine > 0
            && lines.GetLineFromPosition(braces.OpenBraceToken.SpanStart).LineNumber == startLine - 1;
        if (endLine > lastLine + 1 && startLine > 0 && !IsBlank(lines[startLine - 1]) && !aboveIsOpenBrace)
        {
            endLine--;
        }

        var removalEnd = endLine < lines.Count ? lines[endLine].Start : text.Length;
        var changes = new List<TextChange> { new(TextSpan.FromBounds(lines[startLine].Start, removalEnd), string.Empty) };
        var lineBreak = text.ToString(TextSpan.FromBounds(lines[lastLine].End, lines[lastLine].EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            return null;
        }

        if (mode == UsingPlacementMode.Outside)
        {
            // Above the namespace and the comments directly above it, below the file header.
            var insertLine = lines.GetLineFromPosition(ns.SpanStart).LineNumber;
            while (insertLine > 0 && !IsBlank(lines[insertLine - 1]))
            {
                insertLine--;
            }

            if (insertLine == 0 && lines.GetLineFromPosition(ns.SpanStart).LineNumber > 0)
            {
                insertLine = lines.GetLineFromPosition(ns.SpanStart).LineNumber;
            }

            changes.Insert(0, new TextChange(new TextSpan(lines[insertLine].Start, 0), reindented + lineBreak));
            return changes;
        }

        // Inside: on the line below '{', or after a blank line below 'namespace X;'; a blank line before what follows.
        var headerLine = lines.GetLineFromPosition(bodyStart.SpanStart).LineNumber;
        if (headerLine + 1 >= lines.Count || lines.GetLineFromPosition(bodyStart.GetNextToken().SpanStart).LineNumber == headerLine)
        {
            return null;
        }

        var next = lines[headerLine + 1];
        var nextIsClose = ns is NamespaceDeclarationSyntax closing && lines.GetLineFromPosition(closing.CloseBraceToken.SpanStart).LineNumber == headerLine + 1;
        var inserted = (ns is FileScopedNamespaceDeclarationSyntax ? lineBreak : string.Empty)
            + reindented
            + (IsBlank(next) || nextIsClose ? string.Empty : lineBreak);
        changes.Add(new TextChange(new TextSpan(next.Start, 0), inserted));
        return changes;
    }

    /// <summary>
    /// Whether every name, query clause, foreach, await, collection initializer element and deconstruction of the file
    /// binds to the same symbols with the usings moved, and the file has no more errors.
    /// </summary>
    private static bool BindsTheSame(SemanticModel model, CompilationUnitSyntax root, SourceText newText, UsingPlacementMode mode, CancellationToken cancellationToken)
    {
        var newTree = model.SyntaxTree.WithChangedText(newText);
        var newRoot = (CompilationUnitSyntax)newTree.GetRoot(cancellationToken);
        if (newRoot.ContainsDiagnostics)
        {
            return false;
        }

#pragma warning disable RS1030 // The moved usings need a compilation with the changed tree; no speculative model covers using directives.
        var newModel = model.Compilation.ReplaceSyntaxTree(model.SyntaxTree, newTree).GetSemanticModel(newTree);
#pragma warning restore RS1030

        // The usings themselves (in the same order on both sides), then everything else.
        var oldUsings = mode == UsingPlacementMode.Outside ? ((BaseNamespaceDeclarationSyntax)root.Members[0]).Usings : root.Usings;
        var newUsings = mode == UsingPlacementMode.Outside ? newRoot.Usings : ((BaseNamespaceDeclarationSyntax)newRoot.Members[0]).Usings;
        if (oldUsings.Count != newUsings.Count
            || oldUsings.Zip(newUsings, (o, n) => Key(model.GetSymbolInfo(o.NamespaceOrType, cancellationToken)) == Key(newModel.GetSymbolInfo(n.NamespaceOrType, cancellationToken))).Contains(false))
        {
            return false;
        }

        using var oldKeys = Keys(model, root, cancellationToken).GetEnumerator();
        using var newKeys = Keys(newModel, newRoot, cancellationToken).GetEnumerator();
        while (true)
        {
            var hasOld = oldKeys.MoveNext();
            if (hasOld != newKeys.MoveNext())
            {
                return false;
            }

            if (!hasOld)
            {
                break;
            }

            if (oldKeys.Current != newKeys.Current)
            {
                return false;
            }
        }

        var newErrors = newModel.GetDiagnostics(cancellationToken: cancellationToken).Count(d => d.Severity == DiagnosticSeverity.Error);
        return newErrors == 0 || newErrors <= model.GetDiagnostics(cancellationToken: cancellationToken).Count(d => d.Severity == DiagnosticSeverity.Error);
    }

    private static IEnumerable<string> Keys(SemanticModel model, CompilationUnitSyntax root, CancellationToken cancellationToken)
    {
        foreach (var node in root.DescendantNodes(n => n is not UsingDirectiveSyntax))
        {
            switch (node)
            {
                case SimpleNameSyntax or SelectOrGroupClauseSyntax or OrderingSyntax:
                    yield return Key(model.GetSymbolInfo(node, cancellationToken));
                    break;

                case QueryClauseSyntax clause:
                    var info = model.GetQueryClauseInfo(clause, cancellationToken);
                    yield return Key(info.CastInfo) + Key(info.OperationInfo);
                    break;

                case CommonForEachStatementSyntax forEach:
                    var each = model.GetForEachStatementInfo(forEach);
                    yield return Key(each.GetEnumeratorMethod) + Key(each.MoveNextMethod) + Key(each.CurrentProperty);
                    break;

                case AwaitExpressionSyntax awaitExpression:
                    yield return Key(model.GetAwaitExpressionInfo(awaitExpression).GetAwaiterMethod);
                    break;

                case InitializerExpressionSyntax { RawKind: (int)SyntaxKind.CollectionInitializerExpression } initializer:
                    foreach (var element in initializer.Expressions)
                    {
                        yield return Key(model.GetCollectionInitializerSymbolInfo(element, cancellationToken));
                    }

                    break;

                case AssignmentExpressionSyntax { Left: TupleExpressionSyntax or DeclarationExpressionSyntax } deconstruction:
                    yield return Key(model.GetDeconstructionInfo(deconstruction).Method);
                    break;
            }
        }
    }

    private static string Key(SymbolInfo info) =>
        info.Symbol is { } symbol ? Key(symbol) : "?" + info.CandidateReason + string.Join("|", info.CandidateSymbols.Select(Key));

    // A reduced extension method displays as its receiver's member ('int.Twice()'): compare the static method it came from.
    private static string Key(ISymbol? symbol) =>
        symbol is null ? "-"
        : symbol is IMethodSymbol { ReducedFrom: { } reducedFrom } ? "reduced " + Key(reducedFrom)
        : symbol.Kind + ":" + symbol.ToDisplayString(Format) + "@" + symbol.ContainingAssembly?.Identity.Name;

    private static string Indentation(TextLine line)
    {
        var text = line.ToString();
        return text.Substring(0, text.Length - text.TrimStart().Length);
    }

    private static bool IsBlank(TextLine line) => string.IsNullOrWhiteSpace(line.ToString());
}
