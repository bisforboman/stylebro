using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.CodeFixes.Naming;

/// <summary>Fix for BRO1301-BRO1310 and BRO1312: renames to the new name, everywhere the name is used.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CamelCaseNamingCodeFixProvider))]
public sealed class CamelCaseNamingCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            DiagnosticIds.VariableCasing,
            DiagnosticIds.ParameterCasing,
            DiagnosticIds.PrivateFieldNaming,
            DiagnosticIds.InterfacePrefix,
            DiagnosticIds.TypeParameterPrefix,
            DiagnosticIds.FieldPascalCase,
            DiagnosticIds.FieldPrefix,
            DiagnosticIds.FieldUnderscore,
            DiagnosticIds.ElementPascalCase,
            DiagnosticIds.HungarianNotation,
            DiagnosticIds.NamespacePascalCase);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => RenameFixAllProvider.Instance;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Rename to '{newName}'",
                    ct => CamelCaseRenamer.RenameAsync(context.Document.Project.Solution, new[] { (context.Document, diagnostic) }, ct),
                    equivalenceKey: nameof(CamelCaseNamingCodeFixProvider) + diagnostic.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    /// <summary>Collects every diagnostic in the scope and renames them all in one pass.</summary>
    private sealed class RenameFixAllProvider : FixAllProvider
    {
        public static readonly RenameFixAllProvider Instance = new();

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        {
            return new[] { FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution };
        }

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            return Task.FromResult<CodeAction?>(CodeAction.Create(
                fixAllContext.CodeActionEquivalenceKey ?? "Rename all",
                ct => FixAllAsync(fixAllContext, ct),
                fixAllContext.CodeActionEquivalenceKey));
        }

        private static async Task<Solution> FixAllAsync(FixAllContext context, CancellationToken cancellationToken)
        {
            IEnumerable<Document> documents = context.Scope switch
            {
                FixAllScope.Project => context.Project.Documents,
                FixAllScope.Solution => context.Solution.Projects.SelectMany(p => p.Documents),
                _ => context.Document is null ? Array.Empty<Document>() : new[] { context.Document },
            };

            var items = new List<(Document, Diagnostic)>();
            foreach (var document in documents)
            {
                foreach (var diagnostic in await context.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false))
                {
                    items.Add((document, diagnostic));
                }
            }

            return await CamelCaseRenamer.RenameAsync(context.Solution, items, cancellationToken).ConfigureAwait(false);
        }
    }
}
