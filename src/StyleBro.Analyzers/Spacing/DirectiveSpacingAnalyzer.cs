using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Spacing;

/// <summary>BRO1006: no space between '#' and a preprocessor keyword ('# if' -> '#if'). The diagnostic is on the keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectiveSpacingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.DirectiveSpacing);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var (keyword, _) in GetSpaces(c.Tree.GetRoot(c.CancellationToken)))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.DirectiveSpacing, keyword.GetLocation(), keyword.Text));
            }
        });
    }

    /// <summary>
    /// Like StyleCop's SA1006: a '#' followed by whitespace (not a line break) and then a token: the keyword and the
    /// whitespace to remove.
    /// </summary>
    internal static IEnumerable<(SyntaxToken Keyword, TextSpan Space)> GetSpaces(SyntaxNode root)
    {
        foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
        {
            if (!token.IsKind(SyntaxKind.HashToken) || !token.HasTrailingTrivia || token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            {
                continue;
            }

            var keyword = token.GetNextToken(includeDirectives: true);
            if (!keyword.IsKind(SyntaxKind.None) && !keyword.IsMissing && keyword.SpanStart > token.Span.End)
            {
                yield return (keyword, TextSpan.FromBounds(token.Span.End, keyword.SpanStart));
            }
        }
    }
}
