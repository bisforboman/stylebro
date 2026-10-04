using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1132: a comment between a statement's header and its '{'; BRO1134: the same for a declaration. One diagnostic per
/// comment, on the comment.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmbeddedCommentAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmbeddedComment, Descriptors.DeclarationComment);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // A tree action: the comment after the header lies outside the block's span (a node action's diagnostic must not).
        context.RegisterSyntaxTreeAction(
            c =>
            {
                var text = c.Tree.GetText(c.CancellationToken);
                foreach (var node in TreeWalk.Nodes(c.Tree.GetRoot(c.CancellationToken)))
                {
                    foreach (var comment in EmbeddedComments.GetComments(EmbeddedComments.GetOpenBrace(node), text))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmbeddedComment, comment.GetLocation()));
                    }

                    foreach (var comment in EmbeddedComments.GetComments(EmbeddedComments.GetDeclarationOpenBrace(node), text))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.DeclarationComment, comment.GetLocation()));
                    }
                }
            });
    }
}
