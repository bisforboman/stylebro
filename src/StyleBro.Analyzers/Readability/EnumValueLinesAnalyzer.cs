using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1121: each enum value on its own line. The diagnostic is on each value that shares a line.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumValueLinesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EnumValueOnOwnLine);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                foreach (var value in EnumValueLines.GetValuesToMove((EnumDeclarationSyntax)c.Node, text, options))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EnumValueOnOwnLine, value.Identifier.GetLocation(), value.Identifier.ValueText));
                }
            },
            SyntaxKind.EnumDeclaration);
    }
}