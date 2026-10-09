using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1312 (StyleCop SA1300 for namespaces): every part of a namespace declaration's name begins with an upper-case
/// letter, like StyleCop each part of every declaration ('myCompany' and 'data' in 'namespace myCompany.data').
/// Not reported (see <see cref="NamespaceNames"/>): allowed components, a new name already taken in the containing
/// namespace, a namespace another assembly also declares, one declared in generated code, and the project's root
/// namespace or a part of it. The fix renames every declaration and reference in the solution.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamespaceNamingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NamespacePascalCase);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var declaration = (BaseNamespaceDeclarationSyntax)context.Node;
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(declaration.SyntaxTree);
        var allowed = NamespaceNames.ReadAllowed(options);
        options.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        foreach (var part in declaration.Name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            var oldName = part.Identifier.ValueText;
            if (part.Identifier.IsMissing
                || allowed.Contains(oldName)
                || PascalCaseNamingAnalyzer.GetNewName(oldName) is not { } newName
                || NamespaceNames.GetNamespace(context.SemanticModel, part) is not { } declared
                || context.Compilation.GetCompilationNamespace(declared) is not { ContainingNamespace: { } parent } ns)
            {
                continue;
            }

            var fullName = ns.ToDisplayString();
            if (parent.GetMembers(newName).Any()
                || !NamespaceNames.IsOnlyFrom(ns, a => SymbolEqualityComparer.Default.Equals(a, context.Compilation.Assembly))
                || IsInRootNamespace(fullName, rootNamespace)
                || !PublicApi.CanRename(ns, options)
                || declared.Locations.Any(l => l.SourceTree is { } tree && NamespaceNames.IsGenerated(tree)))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.NamespacePascalCase,
                part.Identifier.GetLocation(),
                ImmutableDictionary<string, string?>.Empty
                    .Add(CamelCaseNamingAnalyzer.NewNameKey, newName)
                    .Add(NamespaceNames.NamespaceKey, fullName),
                oldName,
                newName));
        }
    }

    /// <summary>
    /// The project's RootNamespace (default: the project name) is the namespace or inside it. Embedded resource names
    /// start with the root namespace, not with the declared one, and new files get it, so renaming only the code would
    /// leave resources reached by 'typeof(T).Namespace' behind and new files with the old casing. Rename the project
    /// (or set RootNamespace) first.
    /// </summary>
    private static bool IsInRootNamespace(string fullName, string? rootNamespace) =>
        rootNamespace is { Length: > 0 }
            && (rootNamespace == fullName || rootNamespace.StartsWith(fullName + ".", StringComparison.Ordinal));
}
