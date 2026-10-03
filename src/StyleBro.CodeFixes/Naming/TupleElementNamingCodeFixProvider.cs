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

/// <summary>Fix for BRO1311: renames the tuple element name everywhere it's declared and used (<see cref="TupleElementRenamer"/>).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TupleElementNamingCodeFixProvider))]
public sealed class TupleElementNamingCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.TupleElementCasing);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => RenameAllProvider.Instance;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (Rename(diagnostic) is { } rename)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Rename to '{rename.NewName}'",
                        ct => TupleElementRenamer.RenameAsync(context.Document.Project.Solution, new[] { rename }, ct),
                        equivalenceKey: nameof(TupleElementNamingCodeFixProvider)),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static (string OldName, string NewName)? Rename(Diagnostic diagnostic) =>
        diagnostic.Properties.TryGetValue(TupleElementNames.OldNameKey, out var oldName) && oldName is not null
        && diagnostic.Properties.TryGetValue(CamelCaseNamingAnalyzer.NewNameKey, out var newName) && newName is not null
            ? (oldName, newName)
            : null;

    /// <summary>Collects every rename in the scope; each name is renamed once, solution-wide.</summary>
    private sealed class RenameAllProvider : FixAllProvider
    {
        public static readonly RenameAllProvider Instance = new();

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() =>
            new[] { FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution };

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) =>
            Task.FromResult<CodeAction?>(CodeAction.Create(
                "Rename tuple elements",
                ct => FixAllAsync(fixAllContext, ct),
                fixAllContext.CodeActionEquivalenceKey));

        private static async Task<Solution> FixAllAsync(FixAllContext context, CancellationToken cancellationToken)
        {
            IEnumerable<Document> documents = context.Scope switch
            {
                FixAllScope.Project => context.Project.Documents,
                FixAllScope.Solution => context.Solution.Projects.SelectMany(p => p.Documents),
                _ => context.Document is null ? Enumerable.Empty<Document>() : new[] { context.Document },
            };

            var renames = new List<(string, string)>();
            foreach (var document in documents)
            {
                foreach (var diagnostic in await context.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false))
                {
                    if (Rename(diagnostic) is { } rename)
                    {
                        renames.Add(rename);
                    }
                }
            }

            return await TupleElementRenamer.RenameAsync(context.Solution, renames.OrderBy(r => r.Item1, System.StringComparer.Ordinal), cancellationToken).ConfigureAwait(false);
        }
    }
}
