using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1527: auto-accessors on the declaration's line ('{ get; set; }'). The diagnostic is on the '{'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutoAccessorLinesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AutoAccessorsOnOneLine);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var declaration = (BasePropertyDeclarationSyntax)c.Node;
                if (declaration.AccessorList is { } list
                    && AutoAccessorLines.GetChanges(declaration, c.Node.SyntaxTree.GetText(c.CancellationToken), c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree), _ => false) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.AutoAccessorsOnOneLine, list.OpenBraceToken.GetLocation()));
                }
            },
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.IndexerDeclaration);
    }
}
