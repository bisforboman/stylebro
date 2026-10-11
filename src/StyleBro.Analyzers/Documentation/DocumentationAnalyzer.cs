using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    /// <inheritdoc/>
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
            Descriptors.ParameterTagsMatch,
            Descriptors.ParameterTagHasName,
            Descriptors.TypeParameterTagsMatch,
            Descriptors.TypeParameterTagHasName);

    /// <inheritdoc/>
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
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                foreach (var kind in new[] { ParameterDocumentation.TagKind.Parameter, ParameterDocumentation.TagKind.TypeParameter })
                {
                    var kinds = kind == ParameterDocumentation.TagKind.Parameter ? ParameterDocumentation.MemberKinds : ParameterDocumentation.TypeParameterMemberKinds;
                    if (!kinds.Any(k => c.Node.IsKind(k)) || ParameterDocumentation.GetFinding(c.Node, text, kind) is not { } finding)
                    {
                        continue;
                    }

                    foreach (var problem in finding.Problems)
                    {
                        var descriptor = problem.Id switch
                        {
                            DiagnosticIds.ParameterTagHasName => Descriptors.ParameterTagHasName,
                            DiagnosticIds.TypeParameterTagsMatch => Descriptors.TypeParameterTagsMatch,
                            DiagnosticIds.TypeParameterTagHasName => Descriptors.TypeParameterTagHasName,
                            _ => Descriptors.ParameterTagsMatch,
                        };
                        c.ReportDiagnostic(Diagnostic.Create(descriptor, Location.Create(c.Node.SyntaxTree, problem.Span), problem.Message));
                    }
                }
            },
            ParameterDocumentation.MemberKinds.Concat(ParameterDocumentation.TypeParameterMemberKinds).Distinct().ToArray());
        context.RegisterSyntaxTreeAction(c =>
        {
            // One walk over the tree for all four checks: only comments matter to them.
            var trivia = TreeWalk.Trivia(c.Tree.GetRoot(c.CancellationToken))
                .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                    || t.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToList();
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var comment in DocumentationComments.GetMisplacedDocumentationComments(trivia))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.DocumentationSlashesInComment,
                    Location.Create(c.Tree, DocumentationComments.GetReportSpan(comment, text))));
            }

            foreach (var remarks in ParameterDocumentation.GetEmptyRemarks(trivia))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.EmptyRemarks,
                    Location.Create(c.Tree, Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(DocumentationTags.GetStart(remarks), remarks.Span.End))));
            }

            foreach (var placeholder in DocumentationTags.GetPlaceholders(trivia))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.PlaceholderElement, placeholder.StartTag.GetLocation()));
            }

            foreach (var position in DocumentationPeriods.GetMissingPeriods(trivia, c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree)))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.DocumentationEndsWithPeriod, Location.Create(c.Tree, new Microsoft.CodeAnalysis.Text.TextSpan(position, 0))));
            }
        });
    }

    private static void AnalyzeMember(SyntaxNodeAnalysisContext context)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (DocumentationComments.HasDocumentation(context.Node)
            || !DocumentationComments.GeneratesDocumentation(context.Node.SyntaxTree, options)
            || DocumentationComments.HasConditionalDirective((MemberDeclarationSyntax)context.Node))
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

        if (symbols.All(s => s is not null) && DocumentationComments.InheritsDocumentation(symbols!, options))
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
