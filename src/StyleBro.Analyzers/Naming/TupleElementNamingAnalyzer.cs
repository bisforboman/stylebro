using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>BRO1311: element names in tuple types use the configured casing (<see cref="TupleElementNames"/>).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TupleElementNamingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.TupleElementCasing);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TupleElement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var element = (TupleElementSyntax)context.Node;
        if (element.Identifier.IsKind(SyntaxKind.None) || element.Identifier.IsMissing)
        {
            return;
        }

        var name = element.Identifier.ValueText;
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(element.SyntaxTree);
        if (TupleElementNames.GetNewName(name, TupleElementNames.IsCamelCase(options)) is not { } newName
            || TupleElementNames.InheritsNames(element, context.SemanticModel, context.CancellationToken)
            || DisabledCode.Mentions(context.Compilation, name)
            || (!PublicApi.IsRenameAllowed(options) && TupleElementNames.IsInPublicSignature(element, context.SemanticModel, context.CancellationToken)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TupleElementCasing,
            element.Identifier.GetLocation(),
            ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName).Add(TupleElementNames.OldNameKey, name),
            name,
            newName));
    }
}
