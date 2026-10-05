using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1143: an 'else' after an 'if' branch that ends in a jump. The diagnostic is on the 'else' keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ElseAfterJumpAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.ElseAfterJump);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var compilationOptions = start.Compilation.Options;
            start.RegisterSyntaxNodeAction(
                c =>
                {
                    var node = (IfStatementSyntax)c.Node;
                    if (node.Else is null)
                    {
                        return;
                    }

                    var tree = node.SyntaxTree;
                    var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
                    if (ElseAfterJump.IsCandidate(node, tree.GetText(c.CancellationToken), options, id => Severities.IsOn(compilationOptions, tree, id, c.CancellationToken)))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.ElseAfterJump, node.Else.ElseKeyword.GetLocation()));
                    }
                },
                SyntaxKind.IfStatement);
        });
    }
}
