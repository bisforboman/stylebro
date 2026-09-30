using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1105: reports ': base(...)' or ': this(...)' that doesn't start its own line. The diagnostic is on the colon.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstructorInitializerLineAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstructorInitializerLine);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var initializer = (ConstructorInitializerSyntax)c.Node;
                if (ConstructorInitializers.ShouldMove(initializer, initializer.SyntaxTree.GetText(c.CancellationToken)))
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.ConstructorInitializerLine,
                        initializer.ColonToken.GetLocation(),
                        initializer.ThisOrBaseKeyword.Text));
                }
            },
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ThisConstructorInitializer);
    }
}
