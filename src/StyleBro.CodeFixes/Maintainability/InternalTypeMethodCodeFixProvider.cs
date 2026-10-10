using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;
using StyleBro.CodeFixes.Naming;
using StyleBro.CodeFixes.Ordering;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>
/// Fix for BRO1409: <see langword="public"/> -> <see langword="internal"/>. Left alone (the warning stays): a method whose name is a string
/// literal or a <see langword="nameof"/> anywhere in the solution (reflection by name, like the naming rules' guard), and a method
/// of a type that another project derives from (a friend assembly's class can implement an interface with it).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InternalTypeMethodCodeFixProvider))]
public sealed class InternalTypeMethodCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.InternalTypePublicMethod);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            // A method the fix keeps public on purpose offers no action.
            if (root?.FindToken(diagnostic.Location.SourceSpan.Start).Parent is MethodDeclarationSyntax method && model is not null
                && await GetKeptReasonAsync(context.Document, method, model, context.CancellationToken).ConfigureAwait(false) is not null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Declare it internal",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(InternalTypeMethodCodeFixProvider)),
                diagnostic);
        }
    }

    /// <summary>
    /// Why the fix leaves the method public on purpose, or null: its name is in a string or a nameof anywhere in the
    /// solution, or another project derives from its type.
    /// </summary>
    internal static async Task<KeptReason?> GetKeptReasonAsync(Document document, MethodDeclarationSyntax method, SemanticModel model, CancellationToken cancellationToken)
    {
        var names = await CamelCaseRenamer.GetNamesInCodeAsync(document.Project.Solution).ConfigureAwait(false);
        var name = method.Identifier.ValueText;
        return names.Strings.Contains(name) ? KeptReason.NameInString
            : names.Nameofs.Contains(name) ? KeptReason.NameInNameof
            : await HasDerivedTypesElsewhereAsync(model.GetDeclaredSymbol(method, cancellationToken)?.ContainingType, document.Project, cancellationToken).ConfigureAwait(false) ? KeptReason.DerivedTypeInAnotherProject
            : null;
    }

    private static async Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root
            || await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
        {
            return document;
        }

        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics.GroupBy(d => d.Location.SourceSpan).Select(g => g.First()))
        {
            var span = diagnostic.Location.SourceSpan;
            if (span.End > root.FullSpan.End
                || root.FindToken(span.Start).Parent is not MethodDeclarationSyntax method
                || InternalTypeMethods.GetPublicKeyword(method, model, cancellationToken) is not { } keyword
                || keyword.Span != span)
            {
                continue;
            }

            if (await GetKeptReasonAsync(document, method, model, cancellationToken).ConfigureAwait(false) is { } kept)
            {
                KeptFindings.Record(diagnostic, kept);
                continue;
            }

            changes.Add(new TextChange(span, "internal"));
        }

        if (changes.Count == 0)
        {
            return document;
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changed = document.WithText(text.WithChanges(changes));
        if (!Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.MemberOrdering, cancellationToken))
        {
            return changed;
        }

        // BRO1001 sorts public members before internal ones: sort now rather than in another 'dotnet format' run. Each
        // sorted container replaces its span in the original text (like BRO1112's fix), so the edits still merge with
        // other copies' (multi-targeting); the keywords inside it come with the sorted text.
        var sorts = await MemberOrderingCodeFixProvider.GetSortChangesAsync(changed, cancellationToken).ConfigureAwait(false);
        var edits = sorts.Select(s => new TextChange(TextSpan.FromBounds(ToOriginal(s.Span.Start), ToOriginal(s.Span.End)), s.NewText!))
            .Where(s => changes.Any(c => s.Span.Contains(c.Span)))
            .ToList();
        edits.AddRange(changes.Where(c => !edits.Any(e => e.Span.Contains(c.Span))).ToList());
        return document.WithText(text.WithChanges(edits));

        // A position in the changed text, in the original text ('public' -> 'internal' only lengthens).
        int ToOriginal(int position)
        {
            var shift = 0;
            foreach (var change in changes.OrderBy(c => c.Span.Start))
            {
                if (change.Span.Start - shift > position)
                {
                    break;
                }

                shift += change.Span.Length - change.NewText!.Length;
            }

            return position + shift;
        }
    }

    /// <summary>
    /// Whether a type of another project derives from the type: only possible with InternalsVisibleTo, and the analyzer
    /// sees one compilation only (such a type could implement an interface with the inherited method).
    /// </summary>
    private static async Task<bool> HasDerivedTypesElsewhereAsync(INamedTypeSymbol? type, Project project, CancellationToken cancellationToken)
    {
        if (type is null)
        {
            return true;
        }

        if (type.IsSealed || type.IsStatic || type.TypeKind != TypeKind.Class
            || !type.ContainingAssembly.GetAttributes().Any(a => a.AttributeClass?.Name == "InternalsVisibleToAttribute"))
        {
            return false;
        }

        var derived = await SymbolFinder.FindDerivedClassesAsync(type, project.Solution, transitive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return derived.Any(d => d.Locations.Any(l => l.IsInSource && project.GetDocument(l.SourceTree) is null));
    }
}
