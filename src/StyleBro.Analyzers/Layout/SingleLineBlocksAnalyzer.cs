using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1508: a statement's block isn't on a single line. BRO1509: a type, namespace, member body or accessor list isn't
/// on a single line. The diagnostic is on the opening brace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleLineBlocksAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.SingleLineStatementBlock, Descriptors.SingleLineElement);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (SingleLineBlocks.GetBraces(c.Node) is not { } braces)
                {
                    return;
                }

                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                if (!SingleLineBlocks.IsReported(c.Node, text, options))
                {
                    return;
                }

                c.ReportDiagnostic(braces.IsElement
                    ? Diagnostic.Create(Descriptors.SingleLineElement, braces.Open.GetLocation(), GetName(c.Node))
                    : Diagnostic.Create(Descriptors.SingleLineStatementBlock, braces.Open.GetLocation()));
            },
            SingleLineBlocks.Kinds);
    }

    private static string GetName(SyntaxNode node) => node switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        NamespaceDeclarationSyntax ns => ns.Name.ToString(),
        BlockSyntax { Parent: MethodDeclarationSyntax method } => method.Identifier.ValueText,
        BlockSyntax { Parent: ConstructorDeclarationSyntax ctor } => ctor.Identifier.ValueText,
        BlockSyntax { Parent: DestructorDeclarationSyntax dtor } => "~" + dtor.Identifier.ValueText,
        BlockSyntax { Parent: OperatorDeclarationSyntax op } => "operator " + op.OperatorToken.ValueText,
        BlockSyntax { Parent: ConversionOperatorDeclarationSyntax conversion } => "operator " + conversion.Type,
        BlockSyntax { Parent: LocalFunctionStatementSyntax local } => local.Identifier.ValueText,
        AccessorListSyntax { Parent: PropertyDeclarationSyntax property } => property.Identifier.ValueText,
        AccessorListSyntax { Parent: EventDeclarationSyntax @event } => @event.Identifier.ValueText,
        AccessorListSyntax { Parent: IndexerDeclarationSyntax } => "this[]",
        _ => node.Kind().ToString(),
    };
}
