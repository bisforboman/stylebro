using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1101: reports empty statements in blocks and switch sections, and a ';' after the closing brace of a type
/// or namespace (unless <see cref="AllowAfterClosingBraceKey"/> is true). The diagnostic is on the ';' itself, which is all the
/// code fix needs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyStatementAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The ';' after a type's or namespace's '}' is fine (default false): Sonar's S1116 reports empty statements only.</summary>
    public const string AllowAfterClosingBraceKey = "stylebro_allow_semicolon_after_closing_brace";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyStatement);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterNodeAction(
            c =>
            {
                var statement = (EmptyStatementSyntax)c.Node;
                if (EmptyStatements.IsRemovable(statement))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyStatement, statement.SemicolonToken.GetLocation()));
                }
            },
            SyntaxKind.EmptyStatement);

        context.RegisterNodeAction(
            c =>
            {
                var semicolon = EmptyStatements.GetRemovableTrailingSemicolon(c.Node);
                if (semicolon.IsKind(SyntaxKind.SemicolonToken)
                    && !(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree).TryGetValue(AllowAfterClosingBraceKey, out var allow)
                        && bool.TryParse(allow.Trim(), out var allowed) && allowed))
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
