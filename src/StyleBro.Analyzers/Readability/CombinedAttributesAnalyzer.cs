using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1102: reports attribute lists with more than one attribute, on the second attribute.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CombinedAttributesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CombinedAttributes);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var list = (AttributeListSyntax)c.Node;
                if (CombinedAttributes.IsSplittable(list))
                {
                    var second = list.Attributes[1];
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.CombinedAttributes, second.GetLocation(), second.Name.ToString()));
                }
            },
            SyntaxKind.AttributeList);
    }
}
