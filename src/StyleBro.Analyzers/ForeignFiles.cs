using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers;

/// <summary>
/// Files whose findings the suppressors hide (<see cref="PackageFileSuppressor"/>: NuGet packages and git submodules;
/// <see cref="GeneratedFileSuppressor"/>: source generator output; <see cref="OutsideRepositorySuppressor"/>: files outside the
/// repository) aren't analyzed at all: every analyzer used to run on
/// them and the findings were dropped afterwards. 'dotnet format' runs the analyzers once per id it fixes, so
/// Spectre.Console's 281 Polyfill files from the package cache took about half of its netstandard2.0 run (trial 24). Every
/// analyzer's Initialize wraps its context with <see cref="SkipForeignFiles"/>; whether a tree is foreign is decided once
/// per tree.
/// </summary>
internal static class ForeignFiles
{
    private static readonly ConditionalWeakTable<SyntaxTree, StrongBox<bool>> Cache = new();

    private static readonly ConditionalWeakTable<SyntaxNode, MethodDeclarationSyntax[]> PartialBodies = new();

    // The last tree asked on this thread: node actions ask once per node, mostly for the same tree in a row.
    [ThreadStatic]
    private static SyntaxTree? lastTree;

    [ThreadStatic]
    private static bool lastResult;

    /// <summary>The context with every action skipping foreign files.</summary>
    public static AnalysisContext SkipForeignFiles(this AnalysisContext context) => new SkippingContext(context);

    /// <summary>Whether the suppressors would hide every StyleBro finding in <paramref name="tree"/>.</summary>
    public static bool IsForeign(SyntaxTree? tree, AnalyzerOptions options)
    {
        if (tree is null)
        {
            return false;
        }

        if (ReferenceEquals(tree, lastTree))
        {
            return lastResult;
        }

        var result = Cache.GetValue(tree, t => new StrongBox<bool>(Decide(t.FilePath, options))).Value;
        (lastTree, lastResult) = (tree, result);
        return result;
    }

    private static bool Decide(string path, AnalyzerOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var global = options.AnalyzerConfigOptionsProvider.GlobalOptions;
        global.TryGetValue(PackageFileSuppressor.Property, out var packages);
        global.TryGetValue(PackageFileSuppressor.SubmoduleProperty, out var submodules);
        global.TryGetValue(GeneratedFileSuppressor.Property, out var generated);
        global.TryGetValue(OutsideRepositorySuppressor.Property, out var repository);
        return PackageFileSuppressor.IsPackageFile(path, packages)
            || PackageFileSuppressor.IsSubmoduleFile(path, submodules)
            || GeneratedFileSuppressor.IsGeneratedFile(path, generated)
            || OutsideRepositorySuppressor.IsOutside(path, repository);
    }

    private static bool IsForeign(ISymbol symbol, AnalyzerOptions options) =>
        symbol.Locations.Length > 0 && symbol.Locations.All(l => !l.IsInSource || IsForeign(l.SourceTree, options));

    private static Action<T> Skip<T>(Action<T> action, Func<T, bool> isForeign) => c =>
    {
        if (!isForeign(c))
        {
            action(c);
        }
    };

    private static Action<SyntaxNodeAnalysisContext> Skip(Action<SyntaxNodeAnalysisContext> action) =>
        Skip(action, c => IsForeign(c.Node.SyntaxTree, c.Options));

    /// <summary>
    /// Calls a syntax node action for the nodes of hand-written partial method bodies whose declaration a source generator
    /// wrote with [GeneratedCode] (CommunityToolkit.Mvvm's 'partial void OnTitleChanged(string value);', trial 24): the
    /// merged method carries the attribute, so the compiler's driver counts the body as generated code and skips it for node
    /// actions (tree actions still see it). Only such methods: anywhere else the driver calls the action itself.
    /// </summary>
    private static void RegisterGeneratedPartialBodies<TLanguageKindEnum>(CompilationStartAnalysisContext start, Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        where TLanguageKindEnum : struct
    {
        var compilation = start.Compilation;
        var kinds = new HashSet<int>(syntaxKinds.Select(k => Convert.ToInt32(k, CultureInfo.InvariantCulture)));
        start.RegisterSyntaxTreeAction(c =>
        {
            if (IsForeign(c.Tree, c.Options))
            {
                return;
            }

            SemanticModel? model = null;
            foreach (var method in PartialBodies.GetValue(c.Tree.GetRoot(c.CancellationToken), GetPartialBodies))
            {
                // The driver gives analyzers a compilation whose semantic models are cached and shared (RS1030's concern).
#pragma warning disable RS1030
                model ??= compilation.GetSemanticModel(c.Tree);
#pragma warning restore RS1030
                if (model.GetDeclaredSymbol(method, c.CancellationToken) is not { PartialDefinitionPart: not null } symbol || !IsGeneratedByAttribute(symbol))
                {
                    continue;
                }

                foreach (var inside in method.DescendantNodesAndSelf())
                {
                    if (kinds.Contains(inside.RawKind))
                    {
                        // Obsolete for analyzer test hosts (roslyn#63440), not removed: the analyzers' own lambdas take this context.
#pragma warning disable CS0618
                        action(new SyntaxNodeAnalysisContext(inside, symbol, model, c.Options, c.ReportDiagnostic, _ => true, c.CancellationToken));
#pragma warning restore CS0618
                    }
                }
            }
        });
    }

    /// <summary>
    /// The partial methods with a body in the tree, read once for every node action of every analyzer (each one's tree
    /// action used to walk the tree's methods itself; most trees have none).
    /// </summary>
    private static MethodDeclarationSyntax[] GetPartialBodies(SyntaxNode root)
    {
        List<MethodDeclarationSyntax>? found = null;
        foreach (var node in TreeWalk.NodesOfKind(root, SyntaxKind.MethodDeclaration))
        {
            var method = (MethodDeclarationSyntax)node;
            if ((method.Body is not null || method.ExpressionBody is not null) && method.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                (found ??= []).Add(method);
            }
        }

        return found?.ToArray() ?? [];
    }

    /// <summary>
    /// Whether the method has [GeneratedCode], from its generated declaration. (A file that has the attribute itself, a
    /// generated type's, is the driver's generated code: it drops what's reported there.)
    /// </summary>
    private static bool IsGeneratedByAttribute(ISymbol method) =>
        method.GetAttributes().Any(a => a.AttributeClass is { Name: "GeneratedCodeAttribute" } type
            && type.ContainingNamespace.ToDisplayString() == "System.CodeDom.Compiler");

    private sealed class SkippingContext(AnalysisContext inner) : AnalysisContext
    {
        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode) => inner.ConfigureGeneratedCodeAnalysis(analysisMode);

        public override void EnableConcurrentExecution() => inner.EnableConcurrentExecution();

        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action) =>
            inner.RegisterCompilationStartAction(c => action(new SkippingStartContext(c)));

        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action) => inner.RegisterCompilationAction(action);

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) =>
            inner.RegisterSemanticModelAction(Skip(action, c => IsForeign(c.SemanticModel.SyntaxTree, c.Options)));

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) =>
            inner.RegisterSymbolAction(Skip(action, c => IsForeign(c.Symbol, c.Options)), symbolKinds);

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) =>
            inner.RegisterCodeBlockStartAction(Skip(action, c => IsForeign(c.CodeBlock.SyntaxTree, c.Options)));

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) =>
            inner.RegisterCodeBlockAction(Skip(action, c => IsForeign(c.CodeBlock.SyntaxTree, c.Options)));

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) =>
            inner.RegisterSyntaxTreeAction(Skip(action, c => IsForeign(c.Tree, c.Options)));

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            inner.RegisterSyntaxNodeAction(Skip(action), syntaxKinds);
            inner.RegisterCompilationStartAction(start => RegisterGeneratedPartialBodies(start, action, syntaxKinds));
        }
    }

    // RS1012 reads 'inner' as a start context nothing registers on: it's the wrapped one, every action goes to it.
#pragma warning disable RS1012
    private sealed class SkippingStartContext(CompilationStartAnalysisContext inner)
#pragma warning restore RS1012
        : CompilationStartAnalysisContext(inner.Compilation, inner.Options, inner.CancellationToken)
    {
        public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action) => inner.RegisterCompilationEndAction(action);

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) =>
            inner.RegisterSemanticModelAction(Skip(action, c => IsForeign(c.SemanticModel.SyntaxTree, c.Options)));

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) =>
            inner.RegisterSymbolAction(Skip(action, c => IsForeign(c.Symbol, c.Options)), symbolKinds);

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) =>
            inner.RegisterCodeBlockStartAction(Skip(action, c => IsForeign(c.CodeBlock.SyntaxTree, c.Options)));

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) =>
            inner.RegisterCodeBlockAction(Skip(action, c => IsForeign(c.CodeBlock.SyntaxTree, c.Options)));

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) =>
            inner.RegisterSyntaxTreeAction(Skip(action, c => IsForeign(c.Tree, c.Options)));

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            inner.RegisterSyntaxNodeAction(Skip(action), syntaxKinds);
            RegisterGeneratedPartialBodies(inner, action, syntaxKinds);
        }
    }
}
