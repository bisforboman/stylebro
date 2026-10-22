using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// BRO1617 (cref type arguments in braces), BRO1618 ('&lt;see langword&gt;' for a keyword in '&lt;c&gt;') and BRO1619
/// (top-level documentation elements in order), found in one pass over the tree's documentation comments.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentationStyleAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GenericCrefBraces, Descriptors.LangwordElement, Descriptors.DocumentationElementOrder);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context = context.SkipForeignFiles();
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            var text = c.Tree.GetText(c.CancellationToken);

            // Documentation comments are leading trivia: found in the tree's shared trivia array, not by visiting every token.
            foreach (var trivia in TreeWalk.Trivia(root))
            {
                if ((!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    || trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
                {
                    continue;
                }

                foreach (var finding in DocumentationStyle.GetFindings(documentation, text))
                {
                    var descriptor = finding.Id switch
                    {
                        DiagnosticIds.GenericCrefBraces => Descriptors.GenericCrefBraces,
                        DiagnosticIds.LangwordElement => Descriptors.LangwordElement,
                        _ => Descriptors.DocumentationElementOrder,
                    };
                    c.ReportDiagnostic(Diagnostic.Create(descriptor, Location.Create(c.Tree, finding.Span), finding.Argument));
                }
            }
        });
    }
}
