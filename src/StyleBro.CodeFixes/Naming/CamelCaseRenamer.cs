using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
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
        var namespaces = new List<(string, string)>();
        foreach (var (document, diagnostic) in items)
        {
            if (diagnostic.Id == DiagnosticIds.NamespacePascalCase)
            {
                if (diagnostic.Properties.TryGetValue(NamespaceNames.NamespaceKey, out var oldNamespace) && oldNamespace is not null
                    && diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newPart) && newPart is not null)
                {
                    namespaces.Add((oldNamespace, newPart));
                }

                continue;
            }

            if (!diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName) || newName is null
                || !done.Add((GetFileKey(document), diagnostic.Location.SourceSpan.Start)))
            {
                continue;
            }

            // Every copy of the file: '#if' code can hold references that only one target framework sees. All or nothing:
            // when one copy's rename isn't safe (another project sees an override or a clash there), the other copies'
            // edits would rename the shared file without it (Polly: an abstract member renamed, the test project's
            // override left behind).
            var itemChanges = new List<(string File, TextChange Change)>();
            var safe = true;
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
                    || model.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol)
                {
                    continue;
                }

                if ((IsReachableByName(symbol)
                        && IsInStrings(symbol, strings ??= await GetStringLiteralsAsync(solution, cancellationToken).ConfigureAwait(false)))
                    || await GetChangesAsync(solution, symbol, newName, diagnostic.Id == DiagnosticIds.ParameterMatchesBase, cancellationToken).ConfigureAwait(false) is not { } symbolChanges)
                {
                    safe = false;
                    break;
                }

                itemChanges.AddRange(symbolChanges);
            }

            foreach (var (file, change) in safe ? itemChanges : new List<(string File, TextChange Change)>())
            {
                if (!changes.TryGetValue(file, out var list))
                {
                    changes[file] = list = new List<TextChange>();
                }

                list.Add(change);
            }
        }

        await NamespaceRenamer.AddChangesAsync(solution, namespaces, changes, cancellationToken).ConfigureAwait(false);
        var documentsByKey = solution.Projects.SelectMany(p => p.Documents).ToLookup(GetFileKey);
        var documentsByPath = solution.Projects.SelectMany(p => p.Documents).ToLookup(d => d.FilePath ?? d.Id.Id.ToString());
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in changes)
        {
            // Copies of one file whose text differs: every copy gets the first variant's text with its renames (see
            // LinkedFileFixAllProvider); the other variants' edits don't fit that text.
            var path = file.Key.Substring(0, file.Key.LastIndexOf('|'));
            if (!paths.Add(path))
            {
                continue;
            }

            var documents = documentsByKey[file.Key].ToList();
            if (documents.Count == 0)
            {
                continue;
            }

            var original = await documents[0].GetTextAsync(cancellationToken).ConfigureAwait(false);
            var merged = original.WithChanges(LinkedFileFixAllProvider.Merge(file.Value));
            foreach (var document in documentsByPath[path])
            {
                solution = solution.WithDocumentText(document.Id, merged);
            }
        }

        return solution;
    }

    /// <summary>
    /// A physical file and its text: the copies of a multi-targeted file share edits only while their text is the same
    /// ('dotnet format' runs its whitespace and code style fixes first and can leave the copies different; edits found in
    /// one copy don't fit the other's text). See <see cref="LinkedFileFixAllProvider"/>.
    /// </summary>
    internal static string GetFileKey(Document document)
    {
        var text = document.TryGetText(out var loaded) ? loaded : document.GetTextAsync().GetAwaiter().GetResult();
        return (document.FilePath ?? document.Id.Id.ToString()) + "|" + Convert.ToBase64String(text.GetChecksum().ToArray());
    }

    /// <summary>
    /// Every string literal in the solution. A field whose name is one of them is left alone: code can reach a private
    /// field by name through reflection, also from other projects the analyzer can't see (Polly's tests read
    /// '_blockedUntil' with GetField), and renaming it would still compile but break at run time. Its diagnostic stays
    /// for a manual rename. With <paramref name="withNameof"/>, also the names in <c>nameof(...)</c> (BRO1409:
    /// <c>GetMethod(nameof(Run))</c> finds public methods only).
    /// </summary>
    internal static async Task<HashSet<string>> GetStringLiteralsAsync(Solution solution, CancellationToken cancellationToken, bool withNameof = false)
    {
        var strings = new HashSet<string>();
        var documents = new List<Document>();
        foreach (var project in solution.Projects)
        {
            // Source generators' output too: Mapperly reaches private fields by name ('UnsafeAccessor(..., Name = "intValue")').
            documents.AddRange(project.Documents);
            documents.AddRange(await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));
        }

        foreach (var document in documents)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } root)
            {
                foreach (var token in TreeWalk.Tokens(root))
                {
                    if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
                    {
                        strings.Add(token.ValueText);
                    }
                    else if (withNameof && token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"
                        && token.Parent?.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation)
                    {
                        strings.Add(invocation.ArgumentList.Arguments[0].Expression.GetLastToken().ValueText);
                    }
                }
            }
        }

        return strings;
    }

    /// <summary>
    /// Whether code can refer to the symbol by a string: a field by its name (GetField("_count")), a type by its name
    /// or its qualified name (Type.GetType("App.Shape"), "App.Shape, App").
    /// </summary>
    private static bool IsInStrings(ISymbol symbol, HashSet<string> strings)
    {
        // Names that are also data (serializers write public instance fields, properties and enum members by name)
        // are looked for inside strings too, like test JSON.
        // Names that serializers write (public instance fields, properties, events, enum members) are looked for inside
        // strings too, like test JSON. Private fields only by their exact name: other projects reach them only by
        // reflection ('GetField("_count")'), and a common word like 'count' in any message would block every rename.
        // (Inside the field's own type, the analyzer also matches it inside strings: 'DebuggerDisplay("{_count}")'.)
        var isData = symbol is IPropertySymbol or IEventSymbol
            || (symbol is IFieldSymbol field && (FieldNames.IsDataMember(field) || field.ContainingType.TypeKind == TypeKind.Enum));
        if (symbol is not INamedTypeSymbol)
        {
            var word = new System.Text.RegularExpressions.Regex(@"(?<![\w@])" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"(?!\w)");
            return strings.Contains(symbol.Name) || (isData && strings.Any(s => word.IsMatch(s)));
        }

        var pattern = new System.Text.RegularExpressions.Regex(@"(^|\.)" + System.Text.RegularExpressions.Regex.Escape(symbol.Name) + @"($|[,`\[+])");
        return strings.Any(s => pattern.IsMatch(s));
    }

    /// <summary>Symbols whose conflicts are checked by syntax over their member (<see cref="CamelCaseNames.CanRename"/>).</summary>
    private static bool IsMemberScoped(ISymbol symbol) =>
        symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction };

    /// <summary>Symbols that code can reach by a name in a string (reflection, serializers, type names).</summary>
    private static bool IsReachableByName(ISymbol symbol) =>
        symbol is IFieldSymbol or INamedTypeSymbol or IPropertySymbol or IEventSymbol
            || symbol is IMethodSymbol { MethodKind: not MethodKind.LocalFunction };

    /// <summary>Type members whose rename has to follow overrides and implementations.</summary>
    private static bool IsTypeMember(ISymbol symbol) =>
        symbol is IPropertySymbol or IEventSymbol || symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary };

    /// <summary>'IShape.area' (an explicit implementation's name) -> 'area'.</summary>
    private static string SimpleName(string name) => name.Substring(name.LastIndexOf('.') + 1);

    /// <summary>
    /// The edits that rename <paramref name="symbol"/> (and, for a parameter, the same-named parameters of its
    /// overrides and implementations), or null when one of them isn't safe to rename.
    /// </summary>
    private static async Task<List<(string File, TextChange Change)>?> GetChangesAsync(
        Solution solution,
        ISymbol symbol,
        string newName,
        bool keepObservableNames,
        CancellationToken cancellationToken)
    {
        var oldName = symbol.Name;
        var symbols = new List<ISymbol> { symbol };
        if (symbol is IParameterSymbol parameter)
        {
            await AddRelatedAsync(solution, parameter, parameter.ContainingSymbol, parameter.Ordinal, GetParameters, symbols, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is ITypeParameterSymbol { DeclaringMethod: { } method } typeParameter)
        {
            await AddRelatedAsync(solution, typeParameter, method, typeParameter.Ordinal, GetTypeParameters, symbols, cancellationToken).ConfigureAwait(false);
        }
        else if (IsTypeMember(symbol))
        {
            await AddRelatedMembersAsync(solution, symbol, symbols, cancellationToken).ConfigureAwait(false);

            // An implementation that also implements or overrides a member that isn't renamed (another interface's
            // 'Run', a library's) would stop doing so. Compared by name, not symbol: another project's view of a
            // multi-targeted library is a different (retargeted) symbol for the same member.
            var renamed = new HashSet<string>(symbols.Select(s => s.OriginalDefinition.ToDisplayString()));
            if (symbols.Any(s => CamelCaseNamingAnalyzer.GetBaseMembers(s).Any(b => !renamed.Contains(b.OriginalDefinition.ToDisplayString())))
                || await HasDerivedMemberAsync(solution, symbols, newName, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }
        }
        else if (symbol is INamedTypeSymbol type)
        {
            // Constructors and the finalizer carry the type's name.
            symbols.AddRange(type.InstanceConstructors.Concat(type.StaticConstructors)
                .Concat(type.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Destructor))
                .Where(m => !m.IsImplicitlyDeclared));
        }

        var result = new List<(string, TextChange)>();
        foreach (var current in symbols)
        {
            // A related parameter that can't be renamed safely keeps its name (it's then no longer reported either,
            // since the same conflict blocks its own rename); the reported symbol itself was checked by the analyzer.
            if (!SymbolEqualityComparer.Default.Equals(current, symbol)
                && IsMemberScoped(current)
                && (!await CanRenameAsync(current).ConfigureAwait(false)
                    || (keepObservableNames && current is IParameterSymbol && await IsObservableAsync(current).ConfigureAwait(false))))
            {
                continue;
            }

            foreach (var location in current.Locations.Where(l => l.IsInSource))
            {
                var tree = location.SourceTree!;
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var declaration = root.FindToken(location.SourceSpan.Start).Parent;

                // Fields, types and type parameters were checked by their analyzers; references are checked below.
                if (declaration is null
                    || (IsMemberScoped(current) && !CamelCaseNames.CanRename(declaration, oldName, newName))
                    || !await TryAddAsync(solution.GetDocument(tree), location.SourceSpan, newName).ConfigureAwait(false))
                {
                    return null;
                }
            }

            foreach (var referenced in await SymbolFinder.FindReferencesAsync(current, solution, cancellationToken).ConfigureAwait(false))
            {
                // A constructor's references are 'new T(...)' (renamed with the type) and 'this(...)'/'base(...)' (left).
                var isConstructor = referenced.Definition is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor };
                if (SimpleName(referenced.Definition.Name) != oldName && !isConstructor)
                {
                    continue;
                }

                foreach (var reference in referenced.Locations)
                {
                    if (reference.IsImplicit
                        || (isConstructor
                            && (await reference.Document.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString(reference.Location.SourceSpan).TrimStart('@') != oldName))
                    {
                        continue;
                    }

                    var replacement = await GetReplacementAsync(current, reference.Document, reference.Location.SourceSpan, newName, cancellationToken).ConfigureAwait(false);
                    if (replacement is null || !await TryAddAsync(reference.Document, reference.Location.SourceSpan, replacement).ConfigureAwait(false))
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

        // BRO1313: an override's parameter renamed along with its base's keeps a name that reaches run time (nameof).
        async Task<bool> IsObservableAsync(ISymbol related)
        {
            foreach (var location in related.Locations.Where(l => l.IsInSource))
            {
                if (solution.GetDocument(location.SourceTree) is { } document
                    && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } model
                    && (await location.SourceTree!.GetRootAsync(cancellationToken).ConfigureAwait(false)).FindToken(location.SourceSpan.Start).Parent?.Parent?.Parent is { } member
                    && CamelCaseNames.IsNameObservable(member, oldName, model, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        async Task<bool> TryAddAsync(Document? document, TextSpan span, string replacement)
        {
            // Generated code can't be renamed with the rest (a tool writes it again): a Razor page's or a source generator's
            // reference would be left behind.
            if (document is null or SourceGeneratedDocument
                || await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) is not { } tree
                || NamespaceNames.IsGenerated(tree))
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
    /// symbol would hide at that spot, the qualified name ('this.count', or 'Type.count' for a static field). Null for
    /// a type or type parameter whose new name already means something at that spot (the whole rename is skipped).
    /// </summary>
    private static async Task<string?> GetReplacementAsync(
        ISymbol symbol,
        Document document,
        TextSpan span,
        string newName,
        CancellationToken cancellationToken)
    {
        if (symbol is INamedTypeSymbol or ITypeParameterSymbol)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel is null || !semanticModel.LookupSymbols(span.Start, name: newName).IsEmpty ? null : newName;
        }

        if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
        {
            // A simple name ('area()', not 'x.area()') is looked up in scope, where the new name may mean something else.
            var memberRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (memberRoot?.FindToken(span.Start).Parent is IdentifierNameSyntax simple && IsSimpleName(simple)
                && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } memberModel
                && !memberModel.LookupSymbols(span.Start, name: newName).IsEmpty)
            {
                return null;
            }

            return newName;
        }

        if (symbol is not IFieldSymbol field
            || await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || root.FindToken(span.Start).Parent is not IdentifierNameSyntax name
            || !IsSimpleName(name)
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model
            || model.LookupSymbols(span.Start, name: newName) is not { IsEmpty: false } found)
        {
            return newName;
        }

        // Only locals and parameters can be stepped around by qualifying; another member with the new name (for
        // example in a derived type, which would hide the field) means the rename is skipped.
        if (found.Any(s => s is not (ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)))
        {
            return null;
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

    /// <summary>
    /// Adds the overrides and implementations (explicit ones included) of <paramref name="member"/> that have the same
    /// name, recursively: renaming an abstract, virtual or interface member without them wouldn't compile.
    /// </summary>
    private static async Task AddRelatedMembersAsync(Solution solution, ISymbol member, List<ISymbol> symbols, CancellationToken cancellationToken)
    {
        var related = new List<ISymbol>();
        related.AddRange(await SymbolFinder.FindOverridesAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        if (member.ContainingType?.TypeKind == TypeKind.Interface)
        {
            related.AddRange(await SymbolFinder.FindImplementationsAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        foreach (var relatedMember in related)
        {
            if (SimpleName(relatedMember.Name) == SimpleName(member.Name)
                && relatedMember.Locations.Any(l => l.IsInSource)
                && !symbols.Contains(relatedMember, SymbolEqualityComparer.Default))
            {
                symbols.Add(relatedMember);
                await AddRelatedMembersAsync(solution, relatedMember, symbols, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Whether a type deriving from (or implementing) one of the renamed members' types already has a member with the
    /// new name: the renamed member would then clash with it or be hidden by it.
    /// </summary>
    private static async Task<bool> HasDerivedMemberAsync(Solution solution, List<ISymbol> members, string newName, CancellationToken cancellationToken)
    {
        foreach (var type in members.Select(m => m.ContainingType).Where(t => t is not null).Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>())
        {
            var derived = type.TypeKind == TypeKind.Interface
                ? (await SymbolFinder.FindImplementationsAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false)).OfType<INamedTypeSymbol>()
                : await SymbolFinder.FindDerivedClassesAsync(type, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (derived.Any(d => !d.GetMembers(newName).IsEmpty))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds the parameters (or type parameters) at the same position in the overrides and implementations of
    /// <paramref name="member"/> that have the same name, recursively, so they are renamed together.
    /// </summary>
    private static async Task AddRelatedAsync(
        Solution solution,
        ISymbol item,
        ISymbol member,
        int ordinal,
        Func<ISymbol, ImmutableArray<ISymbol>> getItems,
        List<ISymbol> symbols,
        CancellationToken cancellationToken)
    {
        var related = new List<ISymbol>();
        related.AddRange(await SymbolFinder.FindOverridesAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        if (member.ContainingType?.TypeKind == TypeKind.Interface)
        {
            related.AddRange(await SymbolFinder.FindImplementationsAsync(member, solution, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        foreach (var relatedMember in related)
        {
            var items = getItems(relatedMember);
            if (ordinal < items.Length
                && items[ordinal] is { } relatedItem
                && relatedItem.Name == item.Name
                && relatedItem.Locations.Any(l => l.IsInSource)
                && !symbols.Contains(relatedItem, SymbolEqualityComparer.Default))
            {
                symbols.Add(relatedItem);
                await AddRelatedAsync(solution, relatedItem, relatedMember, ordinal, getItems, symbols, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static ImmutableArray<ISymbol> GetParameters(ISymbol member) => member switch
    {
        IMethodSymbol method => method.Parameters.CastArray<ISymbol>(),
        IPropertySymbol property => property.Parameters.CastArray<ISymbol>(),
        _ => ImmutableArray<ISymbol>.Empty,
    };

    private static ImmutableArray<ISymbol> GetTypeParameters(ISymbol member) =>
        member is IMethodSymbol method ? method.TypeParameters.CastArray<ISymbol>() : ImmutableArray<ISymbol>.Empty;
}
