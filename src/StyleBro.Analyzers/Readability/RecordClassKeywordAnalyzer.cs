using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1146: 'record R' instead of 'record class R'. The diagnostic is on the 'class' keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordClassKeywordAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RecordClassKeyword);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var record = (RecordDeclarationSyntax)c.Node;
                if (RecordClassKeywords.GetChange(record) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.RecordClassKeyword, record.ClassOrStructKeyword.GetLocation(), record.Identifier.ValueText));
                }
            },
            SyntaxKind.RecordDeclaration);
    }
}
