using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers;

/// <summary>
/// <c>RegisterSyntaxNodeAction</c> without the binding it costs: the same action, called for the same nodes, from a
/// syntax tree action. A syntax node action makes the compiler's driver compile every method body of the project first
/// (node actions run on its compilation events), even when the action never asks the semantic model anything.
/// <c>dotnet format</c> runs the analyzers once per diagnostic id it fixes, on every project, so that binding was most of
/// a real-world run (Newtonsoft.Json: about 4 s per compilation and id, against 1 s for a tree action). Here the semantic
/// model is the compilation's own (the driver's caching provider hands out the one it shares), created lazily, so an
/// action only binds what it asks about.
/// </summary>
internal static class NodeActions
{
    /// <summary>Calls <paramref name="action"/> for every node of the given kinds, like <c>RegisterSyntaxNodeAction</c>.</summary>
    public static void RegisterNodeAction(this AnalysisContext context, Action<SyntaxNodeAnalysisContext> action, params SyntaxKind[] kinds) =>
        context.RegisterCompilationStartAction(start => start.RegisterNodeAction(action, kinds));

    /// <summary>Calls <paramref name="action"/> for every node of the given kinds, like <c>RegisterSyntaxNodeAction</c>.</summary>
    public static void RegisterNodeAction(this CompilationStartAnalysisContext context, Action<SyntaxNodeAnalysisContext> action, params SyntaxKind[] kinds)
    {
        var compilation = context.Compilation;
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            SemanticModel? model = null;
            foreach (var kind in kinds)
            {
                foreach (var node in TreeWalk.NodesOfKind(root, kind))
                {
                    c.CancellationToken.ThrowIfCancellationRequested();

                    // The driver gives analyzers a compilation whose semantic models are cached and shared (RS1030's concern).
#pragma warning disable RS1030
                    model ??= compilation.GetSemanticModel(c.Tree);
#pragma warning restore RS1030
                    // Obsolete for analyzer test hosts (roslyn#63440), not removed: the analyzers' own lambdas take this context.
#pragma warning disable CS0618
                    action(new SyntaxNodeAnalysisContext(node, model, c.Options, c.ReportDiagnostic, _ => true, c.CancellationToken));
#pragma warning restore CS0618
                }
            }
        });
    }
}
