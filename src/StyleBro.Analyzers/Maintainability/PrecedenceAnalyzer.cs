using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1406/BRO1407: an operand that needs parentheses to show precedence. The diagnostic is on the operand.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrecedenceAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ArithmeticPrecedence, Descriptors.ConditionalPrecedence);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                foreach (var (operand, id) in Precedence.GetFindings(c.Node))
                {
                    var descriptor = id == DiagnosticIds.ArithmeticPrecedence ? Descriptors.ArithmeticPrecedence : Descriptors.ConditionalPrecedence;
                    c.ReportDiagnostic(Diagnostic.Create(descriptor, operand.GetLocation()));
                }
            },
            Precedence.Kinds);
    }
}
