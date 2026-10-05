using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1408: 'enum E : int' and 'class C : object'. The diagnostic is on the base type.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantBaseTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RedundantBaseType);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var declaration = (BaseTypeDeclarationSyntax)c.Node;
                if (declaration.BaseList is { } list
                    && RedundantBaseTypes.GetChange(declaration, c.SemanticModel, c.CancellationToken) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.RedundantBaseType, list.Types[0].GetLocation(), list.Types[0].ToString()));
                }
            },
            SyntaxKind.EnumDeclaration,
            SyntaxKind.ClassDeclaration);
    }
}
