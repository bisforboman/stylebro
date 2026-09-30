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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PrivateFieldNaming, Descriptors.FieldPascalCase, Descriptors.FieldPrefix, Descriptors.FieldUnderscore);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.Locations.FirstOrDefault(l => l.IsInSource) is not { SourceTree: { } tree } location)
        {
            return;
        }

        var style = FieldNames.GetStyle(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree));
        if (FieldNames.GetRename(field, style) is ({ } rule, { } newName) && FieldNames.CanRename(field, newName, style, context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                rule switch
                {
                    FieldRule.PascalCasing => Descriptors.FieldPascalCase,
                    FieldRule.Prefix => Descriptors.FieldPrefix,
                    FieldRule.Underscore => Descriptors.FieldUnderscore,
                    _ => Descriptors.PrivateFieldNaming,
                },
                location,
                ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName),
                field.Name,
                newName));
        }
    }
}
