using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1511: no blank line between documentation and its element. BRO1512: no blank line before the 'while' of
/// 'do ... while'. BRO1513: a blank line before documentation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentationBlankLinesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.BlankLineAfterDocumentation, Descriptors.BlankLineBeforeWhile, Descriptors.BlankLineBeforeDocumentation);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var node in root.DescendantNodes())
            {
                if (node is MemberDeclarationSyntax member and not BaseNamespaceDeclarationSyntax && !member.ContainsDiagnostics)
                {
                    if (DocumentationBlankLines.GetBlankLinesAfterDocumentation(member, text) is { } blank)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAfterDocumentation, Location.Create(c.Tree, new TextSpan(blank.First.Start, 0))));
                    }

                    if (DocumentationBlankLines.GetMissingBlankLine(member, text) is not null)
                    {
                        var documentation = DocumentationBlankLines.GetDocumentation(member);
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineBeforeDocumentation, Location.Create(c.Tree, new TextSpan(documentation.FullSpan.Start, 3))));
                    }
                }
                else if (node is DoStatementSyntax @do && DocumentationBlankLines.GetBlankLinesBeforeWhile(@do, text) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineBeforeWhile, @do.WhileKeyword.GetLocation()));
                }
            }
        });
    }
}
