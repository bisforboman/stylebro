using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1114: one field per declaration. The diagnostic is on the declaration's first token (its attribute if it has one).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CombinedFieldsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CombinedFields);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var declaration = (BaseFieldDeclarationSyntax)c.Node;
                if (CombinedFields.GetChange(declaration, c.Node.SyntaxTree.GetText(c.CancellationToken)) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.CombinedFields,
                        declaration.GetFirstToken().GetLocation(),
                        declaration.Declaration.Variables[1].Identifier.ValueText));
                }
            },
            SyntaxKind.FieldDeclaration,
            SyntaxKind.EventFieldDeclaration);
    }
}