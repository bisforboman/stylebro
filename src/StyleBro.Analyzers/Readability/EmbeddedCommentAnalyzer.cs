using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                // Only braces with a comment right before them can have one to move: found from the tree's comments (few)
                // instead of asking every node's '{' for the token before it.
                var root = c.Tree.GetRoot(c.CancellationToken);
                var braces = new List<SyntaxToken>();
                foreach (var trivia in TreeWalk.Trivia(root))
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                    {
                        var token = trivia.Token;
                        var brace = token.LeadingTrivia.Span.Contains(trivia.Span) ? token : token.GetNextToken();
                        if (brace.IsKind(SyntaxKind.OpenBraceToken) && (braces.Count == 0 || braces[braces.Count - 1] != brace))
                        {
                            braces.Add(brace);
                        }
                    }
                }

                var text = braces.Count > 0 ? c.Tree.GetText(c.CancellationToken) : null;
                foreach (var brace in braces)
                {
                    if (EmbeddedComments.GetOpenBrace(brace.Parent!) == brace)
                    {
                        foreach (var comment in EmbeddedComments.GetComments(brace, text!))
                        {
                            c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmbeddedComment, comment.GetLocation()));
                        }
                    }

                    if (EmbeddedComments.GetDeclarationOpenBrace(brace.Parent!) == brace)
                    {
                        foreach (var comment in EmbeddedComments.GetComments(brace, text!))
                        {
                            c.ReportDiagnostic(Diagnostic.Create(Descriptors.DeclarationComment, comment.GetLocation()));
                        }
                    }
                }
            });
    }
}
