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
                var last = TrailingCommas.GetLastItemWithoutComma(c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken));
                if (last is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.TrailingComma, last.GetLocation()));
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
