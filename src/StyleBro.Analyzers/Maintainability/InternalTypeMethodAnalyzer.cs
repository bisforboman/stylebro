using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1409 (off by default): 'public' on an ordinary method of an internal type. The diagnostic is on 'public'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InternalTypeMethodAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.InternalTypePublicMethod);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var method = (MethodDeclarationSyntax)c.Node;
                if (InternalTypeMethods.GetPublicKeyword(method, c.SemanticModel, c.CancellationToken) is { } keyword)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.InternalTypePublicMethod, keyword.GetLocation(), method.Identifier.ValueText));
                }
            },
            SyntaxKind.MethodDeclaration);
    }
}
