using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1105: reports ': base(...)' or ': this(...)' that doesn't start its own line (or, with
/// stylebro_constructor_initializer_placement = same_line, that can join the parameter list's line). The diagnostic is on
/// the colon.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstructorInitializerLineAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstructorInitializerLine);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var initializer = (ConstructorInitializerSyntax)c.Node;
                var text = initializer.SyntaxTree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                var sameLine = ConstructorInitializers.IsSameLine(options);
                if (sameLine
                    ? ConstructorInitializers.GetJoin(initializer, text, options, id => Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, id, c.CancellationToken), c.SemanticModel, c.CancellationToken) is not null
                    : ConstructorInitializers.ShouldMove(initializer, text))
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.ConstructorInitializerLine,
                        initializer.ColonToken.GetLocation(),
                        initializer.ThisOrBaseKeyword.Text,
                        sameLine ? "on the constructor's line" : "on its own line"));
                }
            },
            SyntaxKind.BaseConstructorInitializer,
            SyntaxKind.ThisConstructorInitializer);
    }
}
