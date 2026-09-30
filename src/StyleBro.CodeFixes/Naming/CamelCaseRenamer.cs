using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// Renames the variables and parameters reported by BRO1301/BRO1302, for the single fix and Fix All alike. All renames
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
                if (declaration is null || !CamelCaseNames.CanRename(declaration, oldName, newName)
                    || !await TryAddAsync(solution.GetDocument(tree), location.SourceSpan).ConfigureAwait(false))
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
                    if (!reference.IsImplicit && !await TryAddAsync(reference.Document, reference.Location.SourceSpan).ConfigureAwait(false))
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

        async Task<bool> TryAddAsync(Document? document, TextSpan span)
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

            result.Add((GetFileKey(document), new TextChange(span, newName)));
            return true;
        }
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
