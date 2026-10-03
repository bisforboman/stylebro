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
        ImmutableArray.Create(Descriptors.VariableCasing, Descriptors.ParameterCasing, Descriptors.HungarianNotation);

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

    private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
    {
        var identifier = GetIdentifier(context.Node);
        if (identifier.IsKind(SyntaxKind.None) || identifier.IsMissing)
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
        if (node.Identifier.IsMissing
            || node.Parent?.Parent is RecordDeclarationSyntax
            || context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is not { } parameter
            || InheritsName(parameter)
            || NamesAMember(parameter)
            || parameter.ContainingSymbol is IMethodSymbol { PartialDefinitionPart: not null } or IMethodSymbol { PartialImplementationPart: not null })
        {
            return;
        }

        Report(context, Descriptors.ParameterCasing, node.Identifier);
    }

    private static void Report(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken identifier)
    {
        var oldName = identifier.ValueText;

        // BRO1310: the Hungarian prefix goes in the same rename ('_iCount' -> 'count'), so one run converges.
        var hungarian = Severities.IsOn(context.Compilation.Options, context.Node.SyntaxTree, DiagnosticIds.HungarianNotation, context.CancellationToken, enabledByDefault: false)
            && !HungarianNames.IsInNativeMethods(context.Node)
            ? HungarianNames.Read(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree))
            : null;
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
