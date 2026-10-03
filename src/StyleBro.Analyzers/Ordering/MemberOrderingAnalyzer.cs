using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// BRO1001: reports at most one diagnostic per type declaration, on the first member that is out of place.
/// One diagnostic per type keeps the code fix simple: fixing it reorders the whole type in one step,
/// which is what makes Fix All (and therefore <c>dotnet format</c>) converge in a single pass.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MemberOrderingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.MemberOrdering);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeType,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.NamespaceDeclaration,
            SyntaxKind.FileScopedNamespaceDeclaration,
            SyntaxKind.CompilationUnit);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var container = context.Node;
        var options = MemberOrderOptions.Read(
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(container.SyntaxTree));

        var violation = MemberOrdering.FindFirstViolation(container, options, MemberOrdering.GetPartialAccess(container, context.SemanticModel, context.CancellationToken));
        if (violation is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.MemberOrdering,
            MemberOrdering.GetNameToken(violation.Member).GetLocation(),
            MemberOrdering.GetDisplayName(violation.Member),
            MemberOrdering.GetDisplayName(violation.ShouldPrecede),
            violation.Reason));
    }
}
