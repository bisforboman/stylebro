using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// Renames the variables, parameters and fields reported by BRO1301/BRO1302/BRO1303, for the single fix and Fix All alike. All renames
/// are computed on the original solution and applied at once, as text edits of the declaration and every reference
/// (named arguments and '&lt;param&gt;' docs included). A parameter's rename cascades to the parameters of overrides
/// and implementations that have the same name, so they don't drift apart. Linked files (multi-targeting) are handled
/// like in <see cref="LinkedFileFixAllProvider"/>: the edits of every copy are merged per physical file.
/// </summary>
internal static class CamelCaseRenamer
{
    public static async Task<Solution> RenameAsync(
        Solution solution,
        IEnumerable<(Document Document, Diagnostic Diagnostic)> items,
        CancellationToken cancellationToken)
    {
        var changes = new Dictionary<string, List<TextChange>>();
        HashSet<string>? strings = null;
        var done = new HashSet<(string File, int Start)>();
        foreach (var (document, diagnostic) in items)
        {
            if (!diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName) || newName is null
                || !done.Add((GetFileKey(document), diagnostic.Location.SourceSpan.Start)))
            {
                continue;
            }

            // Every copy of the file: '#if' code can hold references that only one target framework sees.
            foreach (var id in document.GetLinkedDocumentIds().Add(document.Id))
            {
                var copy = solution.GetDocument(id);
                if (copy is null
                    || await copy.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
                    || await copy.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
                {
                    continue;
                }

                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                if (token.Span != diagnostic.Location.SourceSpan
                    || token.Parent is not { } declaration
                    || model.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol
                    || (symbol is IFieldSymbol && (strings ??= await GetStringLiteralsAsync(solution, cancellationToken).ConfigureAwait(false)).Contains(symbol.Name))
                    || await GetChangesAsync(solution, symbol, newName, cancellationToken).ConfigureAwait(false) is not { } symbolChanges)
                {
                    continue;
                }

                foreach (var (file, change) in symbolChanges)
                {
                    if (!changes.TryGetValue(file, out var list))
                    {
                        changes[file] = list = new List<TextChange>();
                    }

                    list.Add(change);
                }
            }
        }

        var documentsByFile = solution.Projects.SelectMany(p => p.Documents).ToLookup(GetFileKey);
        foreach (var file in changes)
        {
            var documents = documentsByFile[file.Key].ToList();
            if (documents.Count == 0)
            {
                continue;
            }

            var original = await documents[0].GetTextAsync(cancellationToken).ConfigureAwait(false);
            var merged = original.WithChanges(LinkedFileFixAllProvider.Merge(file.Value));
            foreach (var document in documents)
            {
                solution = solution.WithDocumentText(document.Id, merged);
            }
        }

        return solution;
    }

    /// <summary>
    /// Every string literal in the solution. A field whose name is one of them is left alone: code can reach a private
    /// field by name through reflection, also from other projects the analyzer can't see (Polly's tests read
    /// '_blockedUntil' with GetField), and renaming it would still compile but break at run time. Its diagnostic stays
    /// for a manual rename.
    /// </summary>
    private static async Task<HashSet<string>> GetStringLiteralsAsync(Solution solution, CancellationToken cancellationToken)
    {
        var strings = new HashSet<string>();
        foreach (var document in solution.Projects.SelectMany(p => p.Documents))
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } root)
            {
                foreach (var token in root.DescendantTokens())
                {
                    if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
                    {
                        strings.Add(token.ValueText);
                    }
                }
            }
        }

        return strings;
    }

    /// <summary>
    /// The edits that rename <paramref name="symbol"/> (and, for a parameter, the same-named parameters of its
    /// overrides and implementations), or null when one of them isn't safe to rename.
    /// </summary>
    private static async Task<List<(string File, TextChange Change)>?> GetChangesAsync(
        Solution solution,
        ISymbol symbol,
        string newName,
        CancellationToken cancellationToken)
    {
        var oldName = symbol.Name;
        var symbols = new List<ISymbol> { symbol };
        if (symbol is IParameterSymbol parameter)
        {
            await AddRelatedParametersAsync(solution, parameter, symbols, cancellationToken).ConfigureAwait(false);
        }

        var result = new List<(string, TextChange)>();
        foreach (var current in symbols)
        {
            // A related parameter that can't be renamed safely keeps its name (it's then no longer reported either,
            // since the same conflict blocks its own rename); the reported symbol itself was checked by the analyzer.
            if (!SymbolEqualityComparer.Default.Equals(current, symbol) && !await CanRenameAsync(current).ConfigureAwait(false))
            {
                continue;
            }

            foreach (var location in current.Locations.Where(l => l.IsInSource))
            {
                var tree = location.SourceTree!;
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var declaration = root.FindToken(location.SourceSpan.Start).Parent;
                // Fields were checked over their whole type by the analyzer (FieldNames.CanRename).
                if (declaration is null
                    || (current is not IFieldSymbol && !CamelCaseNames.CanRename(declaration, oldName, newName))
                    || !await TryAddAsync(solution.GetDocument(tree), location.SourceSpan, newName).ConfigureAwait(false))
                {
                    return null;
                }
            }

            foreach (var referenced in await SymbolFinder.FindReferencesAsync(current, solution, cancellationToken).ConfigureAwait(false))
            {
                if (referenced.Definition.Name != oldName)
                {
                    continue;
                }

                foreach (var reference in referenced.Locations)
                {
                    if (!reference.IsImplicit
                        && !await TryAddAsync(
                            reference.Document,
                            reference.Location.SourceSpan,
                            await GetReplacementAsync(current, reference.Document, reference.Location.SourceSpan, newName, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
                    {
                        return null;
                    }
                }
            }
        }

        return result;

        async Task<bool> CanRenameAsync(ISymbol related)
        {
            foreach (var location in related.Locations.Where(l => l.IsInSource))
            {
                var root = await location.SourceTree!.GetRootAsync(cancellationToken).ConfigureAwait(false);
                if (root.FindToken(location.SourceSpan.Start).Parent is not { } declaration
                    || !CamelCaseNames.CanRename(declaration, oldName, newName))
                {
                    return false;
                }
            }

            return true;
        }

        async Task<bool> TryAddAsync(Document? document, TextSpan span, string replacement)
        {
            if (document is null)
            {
                return false;
            }

            // Only ever replace the old name itself ('@Name' included).
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text.ToString(span).TrimStart('@') != oldName)
            {
                return false;
            }

            result.Add((GetFileKey(document), new TextChange(span, replacement)));
            return true;
        }
    }

    /// <summary>
    /// The text that replaces a reference: the new name, or for a field whose new name a local, parameter or other
    /// symbol would hide at that spot, the qualified name ('this.count', or 'Type.count' for a static field).
    /// </summary>
    private static async Task<string> GetReplacementAsync(
        ISymbol symbol,
        Document document,
        TextSpan span,
        string newName,
        CancellationToken cancellationToken)
    {
        if (symbol is not IFieldSymbol field
            || await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || root.FindToken(span.Start).Parent is not IdentifierNameSyntax name
            || !IsSimpleName(name)
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model
            || model.LookupSymbols(span.Start, name: newName).IsEmpty)
        {
            return newName;
        }

        var qualifier = field.IsStatic ? field.ContainingType.ToMinimalDisplayString(model, span.Start) : "this";
        return qualifier + "." + newName;
    }

    /// <summary>A name that is looked up in scope, as opposed to the member part of 'x.Name' or 'Name = ...'.</summary>
    private static bool IsSimpleName(IdentifierNameSyntax name)
    {
        return name.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == name => false,
            MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax or XmlNameAttributeSyntax => false,
            QualifiedNameSyntax qualified when qualified.Right == name => false,
            AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment when assignment.Left == name => false,
            _ => true,
        };
    }

    private static async Task AddRelatedParametersAsync(
        Solution solution,
        IParameterSymbol parameter,
        List<ISymbol> symbols,
        CancellationToken cancellationToken)
    {
        var member = parameter.ContainingSymbol;
        var related = new List<ISymbol>();
        related.AddRange(await SymbolFinder.FindOverridesAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        if (member.ContainingType?.TypeKind == TypeKind.Interface)
        {
            related.AddRange(await SymbolFinder.FindImplementationsAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        foreach (var relatedMember in related)
        {
            var parameters = relatedMember switch
            {
                IMethodSymbol method => method.Parameters,
                IPropertySymbol property => property.Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty,
            };

            if (parameter.Ordinal < parameters.Length
                && parameters[parameter.Ordinal] is { } relatedParameter
                && relatedParameter.Name == parameter.Name
                && relatedParameter.Locations.Any(l => l.IsInSource)
                && !symbols.Contains(relatedParameter, SymbolEqualityComparer.Default))
            {
                symbols.Add(relatedParameter);
                await AddRelatedParametersAsync(solution, relatedParameter, symbols, cancellationToken).ConfigureAwait(false);
            }
        }
    }


    private static string GetFileKey(Document document) => document.FilePath ?? document.Id.Id.ToString();
}
