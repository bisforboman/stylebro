using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1145: 'class Marker;' instead of an empty body (C# 12). The diagnostic is on the body's braces.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyTypeBodyAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyTypeBody);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (!EmptyRecordBodies.AllFrameworksAllowTypeBodySemicolon(start.Options.AnalyzerConfigOptionsProvider.GlobalOptions))
            {
                return;
            }

            start.RegisterSyntaxNodeAction(
                c =>
                {
                    var type = (TypeDeclarationSyntax)c.Node;
                    if (EmptyRecordBodies.GetTypeChange(type) is not null)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.EmptyTypeBody,
                            Location.Create(type.SyntaxTree, TextSpan.FromBounds(type.OpenBraceToken.SpanStart, type.CloseBraceToken.Span.End)),
                            type.Identifier.ValueText));
                    }
                },
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.InterfaceDeclaration);
        });
    }
}
