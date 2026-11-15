using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        context = context.SkipForeignFiles();
        context.RegisterCompilationStartAction(start =>
        {
            // Each type's strings and names are read once for all its fields (they were read per field before).
            var facts = new ConcurrentDictionary<INamedTypeSymbol, Lazy<FieldNames.TypeFacts>>(SymbolEqualityComparer.Default);
            var styles = new ConcurrentDictionary<SyntaxTree, FieldStyles>();
            var derived = new Lazy<ILookup<INamedTypeSymbol, INamedTypeSymbol>>(() => GetDerivedTypes(start.Compilation));
            start.RegisterSymbolAction(c => AnalyzeField(c, facts, styles, derived), SymbolKind.Field);
        });
    }

    /// <summary>
    /// The type a field of a private nested type is visible in (the outermost private type's container), whose disabled
    /// code is the only place it can be used besides its own type; null when no containing type is private. Speckle's
    /// 'public double x' in a private struct was skipped for an 'x' in another file's '#if' (trial 25).
    /// </summary>
    private static INamedTypeSymbol? GetPrivateScope(IFieldSymbol field)
    {
        INamedTypeSymbol? scope = null;
        for (var type = field.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.DeclaredAccessibility == Accessibility.Private)
            {
                scope = type.ContainingType;
            }
        }

        return scope;
    }

    /// <summary>Whether the field's name is a word in disabled code where the field can be used (<see cref="GetPrivateScope"/>).</summary>
    private static bool IsInDisabledCode(IFieldSymbol field, SymbolAnalysisContext context) => GetPrivateScope(field) is { } scope
        ? DisabledCode.Mentions(scope, field.Name, context.CancellationToken)
        : DisabledCode.Mentions(context.Compilation, field.Name);

    /// <summary>The compilation's types that have a base class, by that base's definition: read once, for the first field that asks.</summary>
    private static ILookup<INamedTypeSymbol, INamedTypeSymbol> GetDerivedTypes(Compilation compilation)
    {
        var types = new List<INamedTypeSymbol>();
        var pending = new Stack<INamespaceOrTypeSymbol>();
        pending.Push(compilation.Assembly.GlobalNamespace);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var member in current is INamespaceSymbol ns ? ns.GetMembers() : current.GetTypeMembers().CastArray<INamespaceOrTypeSymbol>())
            {
                pending.Push(member);
                if (member is INamedTypeSymbol { TypeKind: TypeKind.Class, BaseType.SpecialType: not SpecialType.System_Object })
                {
                    types.Add((INamedTypeSymbol)member);
                }
            }
        }

        return types.ToLookup<INamedTypeSymbol, INamedTypeSymbol>(t => t.BaseType!.OriginalDefinition, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Whether the field is a protected (or private protected) readonly instance field and a type derived from its type
    /// declares a member with the new name: it would hide the renamed field, and the derived type's 'this.count' would
    /// mean that member. Such a field is used by exactly those types. Derived types in other projects are the fix's to
    /// see, and so are the other fields' (a simple name that would see the member keeps the finding).
    /// </summary>
    private static bool IsHiddenInDerivedType(IFieldSymbol field, string newName, Lazy<ILookup<INamedTypeSymbol, INamedTypeSymbol>> derived)
    {
        if (field is not { DeclaredAccessibility: Accessibility.Protected or Accessibility.ProtectedAndInternal, IsReadOnly: true, IsStatic: false }
            || field.ContainingType.IsSealed)
        {
            return false;
        }

        var pending = new Stack<INamedTypeSymbol>();
        pending.Push(field.ContainingType.OriginalDefinition);
        while (pending.Count > 0)
        {
            foreach (var type in derived.Value[pending.Pop()])
            {
                if (type.MemberNames.Contains(newName))
                {
                    return true;
                }

                pending.Push(type);
            }
        }

        return false;
    }

    private static void AnalyzeField(
        SymbolAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, Lazy<FieldNames.TypeFacts>> cache,
        ConcurrentDictionary<SyntaxTree, FieldStyles> styles,
        Lazy<ILookup<INamedTypeSymbol, INamedTypeSymbol>> derived)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.Locations.FirstOrDefault(l => l.IsInSource) is not { SourceTree: { } tree } location)
        {
            return;
        }

        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        var style = styles.GetOrAdd(tree, t => FieldNames.GetStyles(options, Severities.Configured(context.Compilation.Options, t, "IDE1006", context.CancellationToken, options, "Style")));
        var hungarian = Severities.IsOn(context.Compilation.Options, tree, DiagnosticIds.HungarianNotation, context.CancellationToken, enabledByDefault: false)
            ? HungarianNames.Read(options)
            : null;
        var rename = FieldNames.GetRename(field, style, hungarian);

        // FieldNames' styles follow the naming rules; NamingRules checks the new name against the rule Roslyn picks for the
        // field too (stryker: a '_' rule for private static fields wins over one for static readonly, so '_lockObj' stays).
        if (rename is ({ } rule, { } newName)
            && !PascalCaseNamingAnalyzer.HidesBaseMember(field)
            && PublicApi.CanRename(field, options)
            && !(rule == FieldRule.Prefix && FieldNames.IsPrefixRequiredByNamingRule(field.Name.Substring(0, 2), options))
            && FieldNames.CanRename(field, newName, style, cache.GetOrAdd(field.ContainingType, t => new Lazy<FieldNames.TypeFacts>(() => FieldNames.TypeFacts.For(t, context.CancellationToken))).Value, hungarian)
            && NamingRules.AllowsRename(context.Compilation, options, location, field, newName, context.CancellationToken)
            && !IsHiddenInDerivedType(field, newName, derived)

            // Last: it searches the compilation's disabled code for the name.
            && (field.DeclaredAccessibility == Accessibility.Private || !IsInDisabledCode(field, context)))
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
