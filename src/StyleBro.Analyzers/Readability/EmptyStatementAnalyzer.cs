using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1101: reports empty statements in blocks and switch sections, and a ';' after the closing brace of a type
/// or namespace. The diagnostic is on the ';' itself, which is all the code fix needs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyStatementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyStatement);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            c =>
            {
                var statement = (EmptyStatementSyntax)c.Node;
                if (EmptyStatements.IsRemovable(statement))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyStatement, statement.SemicolonToken.GetLocation()));
                }
            },
            SyntaxKind.EmptyStatement);

        context.RegisterSyntaxNodeAction(
            c =>
            {
                var semicolon = EmptyStatements.GetRemovableTrailingSemicolon(c.Node);
                if (semicolon.IsKind(SyntaxKind.SemicolonToken))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyStatement, semicolon.GetLocation()));
                }
            },
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.NamespaceDeclaration);
    }
}
