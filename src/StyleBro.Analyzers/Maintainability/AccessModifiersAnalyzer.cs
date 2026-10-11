using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1404/BRO1007: a declaration without an access modifier. The diagnostic is on its name.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AccessModifiersAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AccessModifier, Descriptors.PartialAccessModifier);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (AccessModifiers.GetFinding((MemberDeclarationSyntax)c.Node, c.SemanticModel, c.CancellationToken, AccessModifiers.GetPreference(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree))) is { } finding)
                {
                    var descriptor = finding.Id == DiagnosticIds.PartialAccessModifier ? Descriptors.PartialAccessModifier : Descriptors.AccessModifier;
                    c.ReportDiagnostic(Diagnostic.Create(descriptor, finding.Location.GetLocation(), finding.Location.ValueText, finding.Modifier));
                }
            },
            AccessModifiers.Kinds);
    }
}
