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
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ElementPascalCase, Descriptors.AsyncSuffix);

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

    /// <summary>
    /// BRO1314: whether a method returns Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt; or IAsyncEnumerable&lt;T&gt;
    /// (by type, not the awaitable pattern: a custom awaitable says nothing about how callers think of it).
    /// </summary>
    public static bool ReturnsAwaitable(IMethodSymbol method)
    {
        return method.ReturnType is INamedTypeSymbol { ContainingNamespace: { } ns } type
            && (type.Name is "Task" or "ValueTask"
                ? type.Arity <= 1 && ns.ToDisplayString() == "System.Threading.Tasks"
                : type.Name == "IAsyncEnumerable" && type.Arity == 1 && ns.ToDisplayString() == "System.Collections.Generic");
    }

    /// <summary>
    /// BRO1314 (off by default): whether the method should get the 'Async' suffix. Left alone: names that already contain
    /// 'Async' ('ReadAsyncCore'), 'Main', methods with attributes other than a few harmless ones (test frameworks,
    /// '[HttpGet]', '[JSInvokable]', '[OperationContract]' find methods by attribute, some by name), methods of ASP.NET
    /// controllers and SignalR hubs (MVC trims 'Async' from action names; hub clients call methods by name) and event
    /// handlers ('(object sender, EventArgs e)').
    /// </summary>
    public static bool NeedsAsyncSuffix(IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Ordinary
            && method.Name != "Main"
            && method.Name.IndexOf("Async", System.StringComparison.Ordinal) < 0
            && ReturnsAwaitable(method)
            && method.GetAttributes().All(a => a.AttributeClass?.Name is "ObsoleteAttribute" or "MethodImplAttribute" or "DebuggerStepThroughAttribute"
                or "DebuggerHiddenAttribute" or "DebuggerNonUserCodeAttribute" or "EditorBrowsableAttribute" or "SuppressMessageAttribute"
                or "ExcludeFromCodeCoverageAttribute" or "PureAttribute")
            && !IsEventHandler(method)
            && !IsControllerOrHub(method.ContainingType);
    }

    /// <inheritdoc/>
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

    private static bool IsEventHandler(IMethodSymbol method)
    {
        if (method.Parameters.Length != 2 || method.Parameters[0].Type.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        for (var type = method.Parameters[1].Type; type is not null; type = type.BaseType)
        {
            if (type.Name == "EventArgs" && type.ContainingNamespace?.ToDisplayString() == "System")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsControllerOrHub(INamedTypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name is "Controller" or "ControllerBase" or "Hub"
                || current.Name.EndsWith("Controller", System.StringComparison.Ordinal)
                || current.GetAttributes().Any(a => a.AttributeClass?.Name == "ApiControllerAttribute"))
            {
                return true;
            }
        }

        return false;
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, SyntaxNode declaration, SyntaxToken identifier, ISymbol symbol)
    {
        // BRO1314 adds the suffix in the same rename as BRO1309's casing ('getData' -> 'GetDataAsync'), so one run converges.
        var newName = GetNewName(symbol.Name);
        var descriptor = Descriptors.ElementPascalCase;
        var options = context.Compilation.Options;
        if (symbol is IMethodSymbol method
            && ReturnsAwaitable(method)
            && Severities.IsOn(options, declaration.SyntaxTree, DiagnosticIds.AsyncSuffix, context.CancellationToken, enabledByDefault: false)
            && NeedsAsyncSuffix(method))
        {
            var casingOn = Severities.IsOn(options, declaration.SyntaxTree, DiagnosticIds.ElementPascalCase, context.CancellationToken);
            newName = (casingOn ? newName ?? symbol.Name : symbol.Name) + "Async";
            descriptor = Descriptors.AsyncSuffix;
        }

        if (identifier.IsMissing
            || newName is null
            || IsExcluded(symbol)
            || InheritsName(symbol)
            || (symbol is not INamedTypeSymbol && symbol.ContainingType is { } type
                && (FieldNames.HasRelatedMemberName(type, symbol) || (symbol is IPropertySymbol && FieldNames.IsSerialized(type))))
            || IsNameTaken(context, declaration, symbol, newName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
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
