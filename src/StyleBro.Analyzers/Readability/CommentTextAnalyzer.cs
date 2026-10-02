using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1005: one space after '///'. BRO1120: no empty comments at the start or end of a comment.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentTextAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DocumentationLineSpace, Descriptors.EmptyComment);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var span in CommentText.GetBadDocumentationSpaces(root, text))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentationLineSpace, Location.Create(c.Tree, span)));
            }

            foreach (var (reported, _) in CommentText.GetEmptyComments(root, text))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyComment, Location.Create(c.Tree, reported.Span)));
            }
        });
    }
}
