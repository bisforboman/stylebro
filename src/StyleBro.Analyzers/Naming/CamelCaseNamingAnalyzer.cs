using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1301 (variables) and BRO1302 (parameters) begin with a lower-case letter. The diagnostic is on the declaration's
/// name; the new name is in the diagnostic's properties under <see cref="NewNameKey"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CamelCaseNamingAnalyzer : DiagnosticAnalyzer
{
    public const string NewNameKey = "NewName";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.VariableCasing, Descriptors.ParameterCasing, Descriptors.HungarianNotation, Descriptors.ParameterMatchesBase);

    /// <summary>The name token of a variable declaration, for every declaration kind BRO1301 checks.</summary>
    public static SyntaxToken GetIdentifier(SyntaxNode node)
    {
        return node switch
        {
            VariableDeclaratorSyntax v => v.Identifier,
            SingleVariableDesignationSyntax d => d.Identifier,
            ForEachStatementSyntax f => f.Identifier,
            CatchDeclarationSyntax c => c.Identifier,
            FromClauseSyntax f => f.Identifier,
            LetClauseSyntax l => l.Identifier,
            JoinClauseSyntax j => j.Identifier,
            JoinIntoClauseSyntax j => j.Identifier,
            QueryContinuationSyntax q => q.Identifier,
            ParameterSyntax p => p.Identifier,
            _ => default,
        };
    }

    /// <summary>
    /// A constructor parameter named exactly like a property or field of its type ('Point(int X) { this.X = X; }'):
    /// serializers bind constructor parameters to members by name, CsvHelper case-sensitively, so the name is a contract
    /// (a CsvHelper test broke when 'Id' became 'id').
    /// </summary>
    public static bool NamesAMember(IParameterSymbol parameter)
    {
        return parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: { } type }
            && type.GetMembers(parameter.Name).Any(m => m is IPropertySymbol or IFieldSymbol);
    }

    /// <summary>
    /// Whether a parameter keeps the name of the parameter it overrides or implements. Like StyleCop, such a parameter
    /// isn't reported: its name comes from the base, and renaming the base renames it too.
    /// </summary>
    public static bool InheritsName(IParameterSymbol parameter)
    {
        return GetBaseMembers(parameter.ContainingSymbol).Any(b =>
            GetParameters(b) is { } parameters && parameter.Ordinal < parameters.Length && parameters[parameter.Ordinal].Name == parameter.Name);
    }

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeVariable,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.SingleVariableDesignation,
            SyntaxKind.ForEachStatement,
            SyntaxKind.CatchDeclaration,
            SyntaxKind.FromClause,
            SyntaxKind.LetClause,
            SyntaxKind.JoinClause,
            SyntaxKind.JoinIntoClause,
            SyntaxKind.QueryContinuation);
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    /// <summary>The members <paramref name="member"/> overrides or implements (explicitly or implicitly).</summary>
    internal static IEnumerable<ISymbol> GetBaseMembers(ISymbol member)
    {
        switch (member)
        {
            case IMethodSymbol method:
                if (method.OverriddenMethod is { } overridden)
                {
                    yield return overridden;
                }

                foreach (var explicitImplementation in method.ExplicitInterfaceImplementations)
                {
                    yield return explicitImplementation;
                }

                break;

            case IPropertySymbol property:
                if (property.OverriddenProperty is { } overriddenProperty)
                {
                    yield return overriddenProperty;
                }

                foreach (var explicitImplementation in property.ExplicitInterfaceImplementations)
                {
                    yield return explicitImplementation;
                }

                break;

            default:
                yield break;
        }

        // Implicit interface implementations.
        foreach (var @interface in member.ContainingType?.AllInterfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
        {
            foreach (var interfaceMember in @interface.GetMembers(member.Name))
            {
                if (SymbolEqualityComparer.Default.Equals(member.ContainingType!.FindImplementationForInterfaceMember(interfaceMember), member))
                {
                    yield return interfaceMember;
                }
            }
        }
    }

    /// <summary>
    /// BRO1313: the name <paramref name="parameter"/> gets from the members its member overrides or implements, or null
    /// when it has none or they disagree. A base parameter in source counts with the name it will have after its own
    /// rename (its base's, or its BRO1302/BRO1310 name), so a chain of overrides converges in one run. A base parameter
    /// other assemblies see keeps its name unless <paramref name="renamePublicApi"/>.
    /// </summary>
    internal static string? GetBaseName(IParameterSymbol parameter, Func<string, string?> getNewName, bool renamePublicApi, int depth = 0)
    {
        string? result = null;
        foreach (var member in GetBaseMembers(parameter.ContainingSymbol))
        {
            if (GetParameters(member) is not { } parameters || parameter.Ordinal >= parameters.Length)
            {
                continue;
            }

            var baseParameter = parameters[parameter.Ordinal];
            var name = baseParameter.Name;
            if (depth < 8 && baseParameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is { } declaration)
            {
                name = GetBaseName(baseParameter, getNewName, renamePublicApi, depth + 1)
                    ?? (getNewName(name) is { } renamed && (renamePublicApi || !PublicApi.IsVisible(baseParameter))
                        && CamelCaseNames.CanRename(declaration, name, renamed, getNewName) ? renamed : name);
            }

            if (result is not null && result != name)
            {
                return null;
            }

            result = name;
        }

        return result;
    }

    private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
    {
        var identifier = GetIdentifier(context.Node);
        if (identifier.IsKind(SyntaxKind.None) || identifier.IsMissing || HungarianNames.GetVariableName(identifier.ValueText, GetHungarian(context)) is null)
        {
            return;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken);
        if (symbol is ILocalSymbol { IsConst: false } or IRangeVariableSymbol)
        {
            Report(context, Descriptors.VariableCasing, identifier);
        }
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var node = (ParameterSyntax)context.Node;
        if (!node.Identifier.IsMissing && ReportBaseName(context, node))
        {
            return;
        }

        if (node.Identifier.IsMissing
            || HungarianNames.GetVariableName(node.Identifier.ValueText, GetHungarian(context)) is null
            || node.Parent?.Parent is RecordDeclarationSyntax
            || context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is not { } parameter
            || InheritsName(parameter)
            || NamesAMember(parameter)
            || !PublicApi.CanRename(parameter, context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree))
            || parameter.ContainingSymbol is IMethodSymbol { PartialDefinitionPart: not null } or IMethodSymbol { PartialImplementationPart: not null })
        {
            return;
        }

        Report(context, Descriptors.ParameterCasing, node.Identifier);
    }

    /// <summary>
    /// BRO1313 (off by default): a parameter of an override or interface implementation takes the base member's name.
    /// True when reported; otherwise BRO1302 decides. Not reported: discards ('_'), partial methods, bases that disagree,
    /// names that would clash in the member (<see cref="CamelCaseNames.CanRename"/>), names that reach run time
    /// (<see cref="CamelCaseNames.IsNameObservable"/>).
    /// </summary>
    private static bool ReportBaseName(SyntaxNodeAnalysisContext context, ParameterSyntax node)
    {
        if (node.Parent?.Parent is not (MethodDeclarationSyntax or IndexerDeclarationSyntax)
            || node.Identifier.ValueText.Trim('_').Length == 0
            || !Severities.IsOn(context.Compilation.Options, node.SyntaxTree, DiagnosticIds.ParameterMatchesBase, context.CancellationToken, enabledByDefault: false)
            || context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is not { ContainingSymbol: { } member } parameter
            || member is IMethodSymbol { PartialDefinitionPart: not null } or IMethodSymbol { PartialImplementationPart: not null }
            || (!member.IsOverride && member.ContainingType.AllInterfaces.IsEmpty))
        {
            return false;
        }

        var hungarian = GetHungarian(context);
        Func<string, string?> getNewName = name => HungarianNames.GetVariableName(name, hungarian);
        var casingOn = Severities.IsOn(context.Compilation.Options, node.SyntaxTree, DiagnosticIds.ParameterCasing, context.CancellationToken);
        var oldName = parameter.Name;
        var renamePublicApi = PublicApi.IsRenameAllowed(context.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree));
        if ((!renamePublicApi && PublicApi.IsVisible(parameter))
            || GetBaseName(parameter, casingOn ? getNewName : _ => null, renamePublicApi) is not { } newName
            || newName == oldName
            || !CamelCaseNames.IsUsableName(newName)
            || !CamelCaseNames.CanRename(node, oldName, newName, getNewName)
            || CamelCaseNames.IsNameObservable(node.Parent!.Parent!, oldName, context.SemanticModel, context.CancellationToken))
        {
            return false;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ParameterMatchesBase,
            node.Identifier.GetLocation(),
            ImmutableDictionary<string, string?>.Empty.Add(NewNameKey, newName),
            oldName,
            newName));
        return true;
    }

    // BRO1310 (null when off): the Hungarian prefix goes in the same rename ('_iCount' -> 'count'), so one run converges.
    private static HungarianNames? GetHungarian(SyntaxNodeAnalysisContext context) =>
        Severities.IsOn(context.Compilation.Options, context.Node.SyntaxTree, DiagnosticIds.HungarianNotation, context.CancellationToken, enabledByDefault: false)
            && !HungarianNames.IsInNativeMethods(context.Node)
            && !HungarianNames.IsExternParameter(context.Node)
            ? HungarianNames.Read(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree))
            : null;

    private static void Report(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken identifier)
    {
        var oldName = identifier.ValueText;
        var hungarian = GetHungarian(context);
        var newName = HungarianNames.GetVariableName(oldName, hungarian);
        if (newName is not null && newName != CamelCaseNames.GetNewName(oldName))
        {
            descriptor = Descriptors.HungarianNotation;
        }

        if (newName is not null && CamelCaseNames.CanRename(context.Node, oldName, newName, name => HungarianNames.GetVariableName(name, hungarian)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                identifier.GetLocation(),
                ImmutableDictionary<string, string?>.Empty.Add(NewNameKey, newName),
                oldName,
                newName));
        }
    }

    private static ImmutableArray<IParameterSymbol>? GetParameters(ISymbol member)
    {
        return member switch
        {
            IMethodSymbol method => method.Parameters,
            IPropertySymbol property => property.Parameters,
            _ => null,
        };
    }
}
