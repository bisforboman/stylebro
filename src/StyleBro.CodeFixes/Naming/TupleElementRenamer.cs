using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Naming;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// Renames tuple element names for BRO1311. Tuple element names are structural: every '(int count, ...)' in the source
/// is its own declaration, and a use like 't.count' binds to the element of whichever tuple type 't' has. Roslyn's
/// Renamer crashes on tuple elements and SymbolFinder matches elements of different tuple types by name, so a name is
/// renamed by declarations, solution-wide (every element in a tuple type with the old name is reported anyway):
/// <list type="number">
/// <item>every tuple type element with the old name, except in a member that overrides or implements a library member
/// whose own declaration has tuple names (they have to match; 'IComparer&lt;(int a, int b)&gt;.Compare' takes them from
/// the type argument, which is renamed too);</item>
/// <item>every explicit name in a tuple literal ('(count: 1, ...)') converted to a renamed element (else the compiler
/// warns that the name is ignored, CS8123); a literal that keeps its natural type isn't a tuple type the rule checks, and
/// keeps its names;</item>
/// <item>every use ('t.count', 't?.count', 'nameof', '(count: var c)' in a pattern) of an element declared at a renamed
/// place. Inferred names and elements from libraries keep theirs.</item>
/// </list>
/// A rename that adds compile errors anywhere is dropped and its warning stays (a deliberate exception, like the
/// naming rules' reflection guard).
/// </summary>
internal static class TupleElementRenamer
{
    public static async Task<Solution> RenameAsync(Solution solution, IEnumerable<(string OldName, string NewName)> renames, CancellationToken cancellationToken)
    {
        foreach (var (oldName, newName) in renames.Distinct())
        {
            var renamed = await RenameOneAsync(solution, oldName, newName, cancellationToken).ConfigureAwait(false);
            if (renamed is not null && !await AddsErrorsAsync(solution, renamed, cancellationToken).ConfigureAwait(false))
            {
                solution = renamed;
            }
        }

        return solution;
    }

    private static async Task<Solution?> RenameOneAsync(Solution solution, string oldName, string newName, CancellationToken cancellationToken)
    {
        var documents = new List<(Document Document, SyntaxNode Root, SemanticModel Model)>();
        foreach (var document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } root
                && root.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == oldName)
                && await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is { } model)
            {
                documents.Add((document, root, model));
            }
        }

        // Places whose element gets the new name (file|position of the name), and the edits per file.
        var renamed = new HashSet<string>();
        var changes = new Dictionary<string, List<TextChange>>();
        void Edit(Document document, TextChange change)
        {
            var path = document.FilePath ?? document.Name;
            if (!changes.TryGetValue(path, out var list))
            {
                changes[path] = list = new List<TextChange>();
            }

            list.Add(change);
        }

        void Rename(Document document, SyntaxToken token) => Edit(document, new TextChange(token.Span, newName));

        // 1. Tuple type elements.
        foreach (var (document, root, model) in documents)
        {
            foreach (var element in root.DescendantNodes().OfType<TupleElementSyntax>().Where(e => e.Identifier.ValueText == oldName))
            {
                if (!TupleElementNames.InheritsNames(element, model, cancellationToken))
                {
                    renamed.Add(Key(element.Identifier.GetLocation()));
                    Rename(document, element.Identifier);
                }
            }
        }

        // 2. Explicit names in tuple literals, when the element they end up in is renamed (or is their own).
        foreach (var (document, root, model) in documents)
        {
            foreach (var tuple in root.DescendantNodes().OfType<TupleExpressionSyntax>())
            {
                var converted = GetTargetType(tuple, model, cancellationToken) as INamedTypeSymbol;
                for (var i = 0; i < tuple.Arguments.Count; i++)
                {
                    if (tuple.Arguments[i].NameColon is not { } nameColon || nameColon.Name.Identifier.ValueText != oldName)
                    {
                        continue;
                    }

                    var target = converted is { IsTupleType: true } && i < converted.TupleElements.Length ? converted.TupleElements[i] : null;
                    if (target is not null && target.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))
                    {
                        Rename(document, nameColon.Name.Identifier);
                    }
                }
            }
        }

        // 3. Uses of renamed elements.
        foreach (var (document, root, model) in documents)
        {
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == oldName && IsElementUse(n)))
            {
                if (model.GetSymbolInfo(name, cancellationToken).Symbol is IFieldSymbol { ContainingType.IsTupleType: true } element
                    && element.Locations.Any(l => l.IsInSource && renamed.Contains(Key(l))))
                {
                    // '(pair.first, ...)' and 'new { pair.first }' infer a name from the use: it's written out, so the
                    // inferred element (and every use of it) keeps the old name.
                    if (InferredNameStart(name) is { } start)
                    {
                        Edit(document, new TextChange(new TextSpan(start.Position, 0), oldName + start.Separator));
                    }

                    Rename(document, name.Identifier);
                }
            }
        }

        if (changes.Count == 0)
        {
            return null;
        }

        foreach (var file in changes)
        {
            var copies = solution.Projects.SelectMany(p => p.Documents).Where(d => (d.FilePath ?? d.Name) == file.Key).ToList();
            var text = await copies[0].GetTextAsync(cancellationToken).ConfigureAwait(false);
            var merged = text.WithChanges(LinkedFileFixAllProvider.Merge(file.Value.Distinct().ToList()));
            foreach (var copy in copies)
            {
                solution = solution.WithDocumentText(copy.Id, merged);
            }
        }

        return solution;
    }

    /// <summary>
    /// The type a tuple literal is converted to, from its context: GetTypeInfo's ConvertedType (and the natural type) have
    /// elements that point at the literal's own names, so they can't tell a renamed target from none. Null where the
    /// context isn't one of these; the compile check catches a literal left behind (CS8123).
    /// </summary>
    private static ITypeSymbol? GetTargetType(TupleExpressionSyntax tuple, SemanticModel model, CancellationToken cancellationToken)
    {
        var operation = model.GetOperation(tuple, cancellationToken);
        while (operation?.Parent is IConversionOperation or IParenthesizedOperation)
        {
            operation = operation.Parent;
        }

        return operation?.Parent switch
        {
            IConversionOperation conversion => conversion.Type,
            ISimpleAssignmentOperation assignment => assignment.Target.Type,
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } => declarator.Symbol.Type,
            IFieldInitializerOperation field => field.InitializedFields.FirstOrDefault()?.Type,
            IPropertyInitializerOperation property => property.InitializedProperties.FirstOrDefault()?.Type,
            IArgumentOperation argument => argument.Parameter?.Type,
            IReturnOperation => ReturnType(model.GetEnclosingSymbol(tuple.SpanStart, cancellationToken) as IMethodSymbol),
            _ => null,
        };
    }

    private static ITypeSymbol? ReturnType(IMethodSymbol? method) =>
        method is { IsAsync: true, ReturnType: INamedTypeSymbol { TypeArguments.Length: 1 } task } ? task.TypeArguments[0] : method?.ReturnType;

    /// <summary>
    /// Where a tuple argument or anonymous object member gets its name from this use ('(pair.first, ...)', 'new {
    /// pair.first }'), and what goes after the written-out name; null when the use doesn't name anything.
    /// </summary>
    private static (int Position, string Separator)? InferredNameStart(IdentifierNameSyntax name)
    {
        SyntaxNode expression = name;
        while (expression.Parent is ExpressionSyntax parent and not TupleExpressionSyntax && parent.GetLastToken() == name.Identifier)
        {
            expression = parent;
        }

        return expression.Parent switch
        {
            ArgumentSyntax { NameColon: null, Parent: TupleExpressionSyntax } argument => (argument.SpanStart, ": "),
            AnonymousObjectMemberDeclaratorSyntax { NameEquals: null } member => (member.SpanStart, " = "),
            _ => null,
        };
    }

    /// <summary>A name that refers to a tuple element: after '.' or '?.', or the name in a positional pattern.</summary>
    private static bool IsElementUse(IdentifierNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Name == name,
        MemberBindingExpressionSyntax => true,
        NameColonSyntax { Parent: SubpatternSyntax } => true,
        _ => false,
    };

    private static string Key(Location location) => $"{location.SourceTree?.FilePath}|{location.SourceSpan.Start}";

    /// <summary>
    /// Whether any project with a changed document has more compile errors afterwards, or more CS8123 warnings (a tuple
    /// literal's name ignored because the type it converts to was renamed and the literal wasn't).
    /// </summary>
    private static async Task<bool> AddsErrorsAsync(Solution before, Solution after, CancellationToken cancellationToken)
    {
        foreach (var projectId in after.GetChanges(before).GetProjectChanges().Select(c => c.ProjectId))
        {
            var old = await before.GetProject(projectId)!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var updated = await after.GetProject(projectId)!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (Errors(updated) > Errors(old))
            {
                return true;
            }
        }

        return false;
    }

    private static int Errors(Compilation? compilation) =>
        compilation?.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error || d.Id == "CS8123") ?? 0;
}
