using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// BRO1601 (an undocumented override or implementation; the fix adds '/// &lt;inheritdoc/&gt;'), BRO1602 ('///' used
/// for a plain comment; the fix makes it '//'), BRO1603 (documentation text ends with a period), and BRO1604/BRO1605 (a
/// property's summary begins with the words its accessors call for), BRO1606/BRO1607 (constructor and destructor summaries begin with the
/// standard text), BRO1608 (no '&lt;returns&gt;' on void), BRO1609 (no '&lt;placeholder&gt;'), BRO1610 (no empty '&lt;remarks&gt;') and BRO1611
/// ('&lt;param&gt;' tags match the parameters).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Descriptors.InheritDocumentation,
            Descriptors.DocumentationSlashesInComment,
            Descriptors.DocumentationEndsWithPeriod,
            Descriptors.PropertySummaryWording,
            Descriptors.PropertySummaryRestrictedSetter,
            Descriptors.ConstructorSummary,
            Descriptors.DestructorSummary,
            Descriptors.VoidReturnDocumented,
            Descriptors.PlaceholderElement,
            Descriptors.EmptyRemarks,
            Descriptors.ParameterTagsMatch);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMember, DocumentationComments.MemberKinds);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (PropertySummaries.GetFinding((PropertyDeclarationSyntax)c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken)) is { } finding)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        finding.RestrictedSetter ? Descriptors.PropertySummaryRestrictedSetter : Descriptors.PropertySummaryWording,
                        finding.Identifier.GetLocation(),
                        finding.Prefix));
                }
            },
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (ConstructorSummaries.GetFinding((BaseMethodDeclarationSyntax)c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken)) is { } finding)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        finding.IsDestructor ? Descriptors.DestructorSummary : Descriptors.ConstructorSummary,
                        finding.Location));
                }
            },
            ConstructorSummaries.MemberKinds);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                foreach (var returns in DocumentationTags.GetVoidReturns(c.Node))
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.VoidReturnDocumented,
                        Location.Create(c.Node.SyntaxTree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(DocumentationTags.GetStart(returns), returns.Span.End))));
                }
            },
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.DelegateDeclaration);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (ParameterDocumentation.GetFinding(c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken)) is { } finding)
                {
                    foreach (var (tag, message) in finding.Problems)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.ParameterTagsMatch,
                            ParameterDocumentation.GetNameToken(tag)!.Value.GetLocation(),
                            message));
                    }
                }
            },
            ParameterDocumentation.MemberKinds);
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var comment in DocumentationComments.GetMisplacedDocumentationComments(root))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.DocumentationSlashesInComment,
                    Location.Create(c.Tree, DocumentationComments.GetReportSpan(comment, text))));
            }

            foreach (var remarks in ParameterDocumentation.GetEmptyRemarks(root))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.EmptyRemarks,
                    Location.Create(c.Tree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(DocumentationTags.GetStart(remarks), remarks.Span.End))));
            }

            foreach (var placeholder in DocumentationTags.GetPlaceholders(root))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.PlaceholderElement, placeholder.StartTag.GetLocation()));
            }

            foreach (var position in DocumentationPeriods.GetMissingPeriods(root))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentationEndsWithPeriod, Location.Create(c.Tree, new Microsoft.CodeAnalysis.Text.TextSpan(position, 0))));
            }
        });
    }

    private static void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        if (DocumentationComments.HasDocumentation(context.Node))
        {
            return;
        }

        var (identifier, symbols) = context.Node switch
        {
            EventFieldDeclarationSyntax field => (
                field.Declaration.Variables.First().Identifier,
                field.Declaration.Variables.Select(v => context.SemanticModel.GetDeclaredSymbol(v, context.CancellationToken)).ToList()),
            _ => (GetIdentifier(context.Node), new List<ISymbol?> { context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken) }),
        };

        if (symbols.All(s => s is not null) && DocumentationComments.InheritsDocumentation(symbols!))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.InheritDocumentation, identifier.GetLocation(), identifier.ValueText));
        }
    }

    private static SyntaxToken GetIdentifier(SyntaxNode member) => member switch
    {
        MethodDeclarationSyntax method => method.Identifier,
        PropertyDeclarationSyntax property => property.Identifier,
        IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
        EventDeclarationSyntax @event => @event.Identifier,
        _ => member.GetFirstToken(),
    };
}
