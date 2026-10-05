using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1140: 'record R(int X);' instead of an empty body. The diagnostic is on the body's braces.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyRecordBodyAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyRecordBody);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var record = (RecordDeclarationSyntax)c.Node;
                if (EmptyRecordBodies.GetChange(record) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.EmptyRecordBody,
                        Location.Create(record.SyntaxTree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(record.OpenBraceToken.SpanStart, record.CloseBraceToken.Span.End)),
                        record.Identifier.ValueText));
                }
            },
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }
}
