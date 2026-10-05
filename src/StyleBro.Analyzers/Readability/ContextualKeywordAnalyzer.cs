using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1144: '@field', '@extension', '@partial' where C# 14 would read the identifier as a keyword. 'field' is looked for
/// in property accessors only (a node action, with the semantic model at hand for C# 14 code); 'extension' and 'partial'
/// in one pass over the file's tokens.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContextualKeywordAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ContextualKeyword);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var property = (PropertyDeclarationSyntax)c.Node;
                SyntaxNode? body = (SyntaxNode?)property.AccessorList ?? property.ExpressionBody;
                if (body is null)
                {
                    return;
                }

                foreach (var token in body.DescendantTokens())
                {
                    if (token.Text == "field" && ContextualKeywords.ShouldEscape(token, () => c.SemanticModel))
                    {
                        Report(token, c.ReportDiagnostic);
                    }
                }
            },
            SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var token in TreeWalk.Tokens(c.Tree.GetRoot(c.CancellationToken)))
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && ContextualKeywords.IsCandidate(token) && token.Text != "field"
                    && ContextualKeywords.ShouldEscape(token, () => null))
                {
                    Report(token, c.ReportDiagnostic);
                }
            }
        });
    }

    private static void Report(SyntaxToken token, System.Action<Diagnostic> report) =>
        report(Diagnostic.Create(Descriptors.ContextualKeyword, token.GetLocation(), token.Text));
}
