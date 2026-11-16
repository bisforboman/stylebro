using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1311: element names in tuple types and explicit names in tuple literals use the configured casing
/// (<see cref="TupleElementNames"/>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TupleElementNamingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.TupleElementCasing);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context = context.SkipForeignFiles();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TupleElement);
        context.RegisterSyntaxNodeAction(AnalyzeLiteral, SyntaxKind.TupleExpression);
    }

    /// <summary>
    /// The explicit names of a tuple literal ('(count: 1, name: "a")'), like StyleCop 1.3's SA1316: always checked, but
    /// for a name that follows a declared tuple type's (that declaration is reported, and its fix renames the literal
    /// too; a library's or a public signature's names can't change) and one whose new name another element has.
    /// </summary>
    private static void AnalyzeLiteral(SyntaxNodeAnalysisContext context)
    {
        var tuple = (TupleExpressionSyntax)context.Node;
        AnalyzerConfigOptions? options = null;
        for (var i = 0; i < tuple.Arguments.Count; i++)
        {
            if (tuple.Arguments[i].NameColon is not { Name.Identifier: { IsMissing: false } identifier })
            {
                continue;
            }

            var name = identifier.ValueText;
            options ??= context.Options.AnalyzerConfigOptionsProvider.GetOptions(tuple.SyntaxTree);
            if (TupleElementNames.GetNewName(name, TupleElementNames.IsCamelCase(options)) is not { } newName
                || TupleElementNames.HasNameClash(tuple, i, newName)
                || TupleElementNames.ClassifyLiteralElement(tuple, i, context.SemanticModel, context.CancellationToken).Kind == TupleLiteralElement.FollowsTarget
                || DisabledCode.Mentions(context.Compilation, identifier.ValueText)
                || (context.ContainingSymbol is { } owner
                    && !NamingRules.AllowsRename(context.Compilation, options, identifier.GetLocation(), owner, null, context.CancellationToken)))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.TupleElementCasing,
                identifier.GetLocation(),
                ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName).Add(TupleElementNames.OldNameKey, name),
                name,
                newName));
        }
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var element = (TupleElementSyntax)context.Node;
        if (element.Identifier.IsKind(SyntaxKind.None) || element.Identifier.IsMissing)
        {
            return;
        }

        var name = element.Identifier.ValueText;
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(element.SyntaxTree);
        if (TupleElementNames.GetNewName(name, TupleElementNames.IsCamelCase(options)) is not { } newName
            || TupleElementNames.InheritsNames(element, context.SemanticModel, options, context.CancellationToken)
            || DisabledCode.Mentions(context.Compilation, name)
            || (!PublicApi.IsRenameAllowed(options) && TupleElementNames.IsInPublicSignature(element, context.SemanticModel, context.CancellationToken))
            || (context.ContainingSymbol is { } owner
                && !NamingRules.AllowsRename(context.Compilation, options, element.Identifier.GetLocation(), owner, null, context.CancellationToken)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TupleElementCasing,
            element.Identifier.GetLocation(),
            ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName).Add(TupleElementNames.OldNameKey, name),
            name,
            newName));
    }
}
