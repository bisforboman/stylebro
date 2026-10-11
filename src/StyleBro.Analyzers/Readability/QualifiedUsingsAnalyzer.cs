using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1126: using directives inside a namespace are fully qualified. The diagnostic is on the directive.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QualifiedUsingsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.QualifiedUsing);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (QualifiedUsings.GetQualifiedName((UsingDirectiveSyntax)c.Node, c.SemanticModel, c.CancellationToken) is { } name)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.QualifiedUsing, c.Node.GetLocation(), name));
                }
            },
            SyntaxKind.UsingDirective);
    }
}
