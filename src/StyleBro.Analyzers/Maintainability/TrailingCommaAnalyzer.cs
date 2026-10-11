using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1401: reports the last item of a multi-line initializer, enum or switch expression without a trailing comma.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrailingCommaAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TrailingComma);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var tree = c.Node.SyntaxTree;
                if (TrailingCommas.GetFinding(c.Node, tree.GetText(c.CancellationToken), () => c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree)) is { } finding)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.TrailingComma,
                        finding.Location,
                        finding.Change.Span.Length > 0 ? "Remove the trailing comma" : "Add a trailing comma"));
                }
            },
            SyntaxKind.ArrayInitializerExpression,
            SyntaxKind.ObjectInitializerExpression,
            SyntaxKind.CollectionInitializerExpression,
            SyntaxKind.WithInitializerExpression,
            SyntaxKind.AnonymousObjectCreationExpression,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.SwitchExpression);
    }
}
