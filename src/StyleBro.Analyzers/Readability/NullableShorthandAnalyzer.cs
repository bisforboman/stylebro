using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1115: 'int?' instead of 'Nullable&lt;int&gt;'. The diagnostic is on the whole (qualified) type name.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullableShorthandAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NullableShorthand);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (NullableShorthand.GetFinding((GenericNameSyntax)c.Node, c.SemanticModel, c.CancellationToken) is { } finding)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.NullableShorthand, finding.Node.GetLocation(), finding.Argument.ToString()));
                }
            },
            SyntaxKind.GenericName);
    }
}