using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1304 (interface names begin with I) and BRO1305 (type parameter names begin with T). Reported on every
/// declaration (each part of a partial interface), like StyleCop; the new name is in the properties under
/// <see cref="CamelCaseNamingAnalyzer.NewNameKey"/>, and the fix is the shared rename.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrefixNamingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.InterfacePrefix, Descriptors.TypeParameterPrefix);

    /// <summary>
    /// Whether a method's type parameter keeps the name of the one it overrides or implements. Like a parameter
    /// (<see cref="CamelCaseNamingAnalyzer.InheritsName"/>), it isn't reported: renaming the base renames it too.
    /// </summary>
    public static bool InheritsName(ITypeParameterSymbol typeParameter)
    {
        return typeParameter.DeclaringMethod is { } method
            && CamelCaseNamingAnalyzer.GetBaseMembers(method).OfType<IMethodSymbol>().Any(b =>
                typeParameter.Ordinal < b.TypeParameters.Length && b.TypeParameters[typeParameter.Ordinal].Name == typeParameter.Name);
    }

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInterface, SyntaxKind.InterfaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeParameter, SyntaxKind.TypeParameter);
    }

    private static void AnalyzeInterface(SyntaxNodeAnalysisContext context)
    {
        var node = (InterfaceDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is { } symbol
            && PrefixNames.GetInterfaceName(symbol.Name) is { } newName
            && !PrefixNames.IsTypeNameTaken(newName, context.SemanticModel.Compilation)
            && context.SemanticModel.LookupSymbols(node.SpanStart, name: newName).IsEmpty)
        {
            Report(context, Descriptors.InterfacePrefix, node.Identifier, newName);
        }
    }

    private static void AnalyzeTypeParameter(SyntaxNodeAnalysisContext context)
    {
        var node = (TypeParameterSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) is { } symbol
            && PrefixNames.GetTypeParameterName(symbol.Name) is { } newName
            && !InheritsName(symbol)
            && symbol.DeclaringMethod is not ({ PartialDefinitionPart: not null } or { PartialImplementationPart: not null })
            && !PrefixNames.IsTypeNameTaken(newName, context.SemanticModel.Compilation)
            && context.SemanticModel.LookupSymbols(node.SpanStart, name: newName).IsEmpty)
        {
            Report(context, Descriptors.TypeParameterPrefix, node.Identifier, newName);
        }
    }

    private static void Report(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken identifier, string newName)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            identifier.GetLocation(),
            ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName),
            identifier.ValueText,
            newName));
    }
}
