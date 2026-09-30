using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1309 (StyleCop SA1300): types, methods, properties, events, enum members and local functions begin with an
/// upper-case letter. Interfaces are BRO1304's, fields BRO1303/BRO1306-BRO1308's, and namespaces are left out
/// (renaming them also changes resource names and breaks folder conventions). The new name is in the properties
/// under <see cref="CamelCaseNamingAnalyzer.NewNameKey"/>; the fix is the shared rename.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PascalCaseNamingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ElementPascalCase);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.EnumMemberDeclaration,
            SyntaxKind.LocalFunctionStatement);
        context.RegisterSyntaxNodeAction(AnalyzeEventField, SyntaxKind.EventFieldDeclaration);
    }

    /// <summary>'lowerMethod' -> 'LowerMethod', '_helper' -> 'Helper'; null when it already begins with a capital.</summary>
    public static string? GetNewName(string name)
    {
        var core = name.TrimStart('_');
        if (core.Length == 0 || !char.IsLetter(core[0]) || (core == name && char.IsUpper(name[0])))
        {
            return null;
        }

        var result = char.ToUpperInvariant(core[0]) + core.Substring(1);
        return result != name && SyntaxFacts.IsValidIdentifier(result) ? result : null;
    }

    /// <summary>
    /// Whether a member keeps the name of the member it overrides or implements (explicit implementations always do).
    /// Like StyleCop, such a member isn't reported: renaming the base renames it too.
    /// </summary>
    public static bool InheritsName(ISymbol member)
    {
        return member switch
        {
            IMethodSymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
            IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
            IEventSymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
            _ => CamelCaseNamingAnalyzer.GetBaseMembers(member).Any(b => b.Name == member.Name)
                || (member is IEventSymbol { OverriddenEvent: { } overridden } && overridden.Name == member.Name),
        };
    }

    private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var identifier = context.Node switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier,
            MethodDeclarationSyntax method => method.Identifier,
            PropertyDeclarationSyntax property => property.Identifier,
            EventDeclarationSyntax @event => @event.Identifier,
            EnumMemberDeclarationSyntax member => member.Identifier,
            LocalFunctionStatementSyntax local => local.Identifier,
            _ => default,
        };

        if (context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken) is { } symbol)
        {
            Analyze(context, context.Node, identifier, symbol);
        }
    }

    private static void AnalyzeEventField(SyntaxNodeAnalysisContext context)
    {
        foreach (var variable in ((EventFieldDeclarationSyntax)context.Node).Declaration.Variables)
        {
            if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is { } symbol)
            {
                Analyze(context, variable, variable.Identifier, symbol);
            }
        }
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, SyntaxNode declaration, SyntaxToken identifier, ISymbol symbol)
    {
        if (identifier.IsMissing
            || GetNewName(symbol.Name) is not { } newName
            || IsExcluded(symbol)
            || InheritsName(symbol)
            || (symbol is not INamedTypeSymbol && symbol.ContainingType is { } type
                && (FieldNames.HasRelatedMemberName(type, symbol) || (symbol is IPropertySymbol && FieldNames.IsSerialized(type))))
            || IsNameTaken(context, declaration, symbol, newName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ElementPascalCase,
            identifier.GetLocation(),
            ImmutableDictionary<string, string?>.Empty.Add(CamelCaseNamingAnalyzer.NewNameKey, newName),
            symbol.Name,
            newName));
    }

    /// <summary>
    /// Members whose name the outside world depends on in ways the rename can't follow: external methods (a P/Invoke
    /// name is the native entry point), properties and enum members with attributes (serialization), and partial
    /// methods (both parts must change together). Interfaces (BRO1304) and record parameters (no property
    /// declaration) never get here.
    /// </summary>
    private static bool IsExcluded(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.IsExtern
                || method.GetAttributes().Any(a => a.AttributeClass?.Name is "DllImportAttribute" or "LibraryImportAttribute")
                || method.PartialDefinitionPart is not null
                || method.PartialImplementationPart is not null
                || method.MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction or MethodKind.DelegateInvoke),
            IPropertySymbol property => property.IsIndexer || property.GetAttributes().Length > 0,
            IFieldSymbol field => field.GetAttributes().Length > 0,
            _ => false,
        };
    }

    /// <summary>
    /// Whether the new name is already in use: anything visible where the symbol is declared (members of the type and
    /// its base types, types, namespaces), or for a type, a type anywhere in the compilation's source. A local function
    /// is checked like a local, over its containing member.
    /// </summary>
    private static bool IsNameTaken(SyntaxNodeAnalysisContext context, SyntaxNode declaration, ISymbol symbol, string newName)
    {
        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction })
        {
            return !CamelCaseNames.CanRename(declaration, symbol.Name, newName);
        }


        return (symbol is INamedTypeSymbol && PrefixNames.IsTypeNameTaken(newName, context.SemanticModel.Compilation))
            || !context.SemanticModel.LookupSymbols(declaration.SpanStart, name: newName).IsEmpty;
    }
}
