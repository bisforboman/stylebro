using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1303: private field names are camelCase ('count') or, with stylebro_private_field_naming = _camelCase,
/// '_count'. BRO1306: constants, static readonly and non-private fields are PascalCase ('MaxCount'). BRO1307: no
/// 'm_', 's_' or 't_' prefix. BRO1308: no underscore inside the name. Each field gets at most one of them. The diagnostic
/// is on the field's name; the new name is in the properties under <see cref="CamelCaseNamingAnalyzer.NewNameKey"/>,
/// and the fix is the same rename as for BRO1301/BRO1302.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FieldNamingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PrivateFieldNaming, Descriptors.FieldPascalCase, Descriptors.FieldPrefix, Descriptors.FieldUnderscore, Descriptors.HungarianNotation);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            // Each type's strings and names are read once for all its fields (they were read per field before).
            var facts = new ConcurrentDictionary<INamedTypeSymbol, Lazy<FieldNames.TypeFacts>>(SymbolEqualityComparer.Default);
            var styles = new ConcurrentDictionary<SyntaxTree, FieldStyles>();
            start.RegisterSymbolAction(c => AnalyzeField(c, facts, styles), SymbolKind.Field);
        });
    }

    private static void AnalyzeField(
        SymbolAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, Lazy<FieldNames.TypeFacts>> cache,
        ConcurrentDictionary<SyntaxTree, FieldStyles> styles)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.Locations.FirstOrDefault(l => l.IsInSource) is not { SourceTree: { } tree } location)
        {
            return;
        }

        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        var style = styles.GetOrAdd(tree, _ => FieldNames.GetStyles(options));
        var hungarian = Severities.IsOn(context.Compilation.Options, tree, DiagnosticIds.HungarianNotation, context.CancellationToken, enabledByDefault: false)
            ? HungarianNames.Read(options)
            : null;
        var rename = FieldNames.GetRename(field, style, hungarian);
        if (rename is ({ } rule, { } newName)
            && !(rule == FieldRule.Prefix && FieldNames.IsPrefixRequiredByNamingRule(field.Name.Substring(0, 2), options))
            && FieldNames.CanRename(field, newName, style, cache.GetOrAdd(field.ContainingType, t => new Lazy<FieldNames.TypeFacts>(() => FieldNames.TypeFacts.For(t, context.CancellationToken))).Value, hungarian))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                rule switch
                {
                    FieldRule.PascalCasing => Descriptors.FieldPascalCase,
                    FieldRule.Prefix => Descriptors.FieldPrefix,
                    FieldRule.Underscore => Descriptors.FieldUnderscore,
                    FieldRule.Hungarian => Descriptors.HungarianNotation,
                    _ => Descriptors.PrivateFieldNaming,
                },
                location,
                ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName),
                field.Name,
                newName));
        }
    }
}
