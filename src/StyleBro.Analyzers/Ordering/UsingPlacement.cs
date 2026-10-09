using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Readability;

namespace StyleBro.Analyzers.Ordering;

/// <summary>Where using directives go: the SDK's csharp_using_directive_placement.</summary>
internal enum UsingPlacementMode
{
    /// <summary>'inside_namespace', or not set (StyleCop's default).</summary>
    Inside,

    /// <summary>'outside_namespace'.</summary>
    Outside,

    /// <summary>Any other value: nothing is checked.</summary>
    Preserve,
}

/// <summary>
/// Shared logic for BRO1008 (StyleCop SA1200): using directives inside or outside the namespace. The usings of a file
/// move together, and only when no name in the file can be looked up differently afterwards: moving a using changes
/// where lookup finds what it imports (the SDK's IDE0065 fix made 'Order' ambiguous in eShop and turned a type into a
/// namespace in Ocelot).
/// </summary>
internal static class UsingPlacement
{
    public const string ConfigKey = "csharp_using_directive_placement";

    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<INamespaceOrTypeSymbol, ILookup<string, IMethodSymbol>>> ExtensionCache = new();

    // Methods the compiler looks up by name without a name in the code (foreach, await, collection initializers,
    // deconstruction, query clauses): an extension method with one of these names counts as used.
    private static readonly string[] ImplicitMethodNames =
    {
        "GetEnumerator", "GetAsyncEnumerator", "GetAwaiter", "Add", "Deconstruct", "Select", "SelectMany", "Where", "Join",
        "GroupJoin", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "GroupBy", "Cast",
    };

    public static UsingPlacementMode GetMode(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(ConfigKey, out var value))
        {
            return UsingPlacementMode.Inside;
        }

        return value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "inside_namespace" => UsingPlacementMode.Inside,
            "outside_namespace" => UsingPlacementMode.Outside,
            _ => UsingPlacementMode.Preserve,
        };
    }

    /// <summary>The usings that are in the wrong place: the cheap syntax check before <see cref="GetChanges"/>.</summary>
    public static ImmutableArray<UsingDirectiveSyntax> GetMisplaced(CompilationUnitSyntax root, UsingPlacementMode mode) =>
        mode switch
        {
            UsingPlacementMode.Outside => root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .OfType<BaseNamespaceDeclarationSyntax>().SelectMany(n => n.Usings).ToImmutableArray(),
            UsingPlacementMode.Inside when root.Members.Any(m => m is BaseNamespaceDeclarationSyntax) =>
                root.Usings.Where(u => !u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)).ToImmutableArray(),
            _ => ImmutableArray<UsingDirectiveSyntax>.Empty,
        };

    /// <summary>
    /// The edits that move every using of the file to its place, or null when that can't be done safely: not exactly
    /// one namespace and nothing else in the file, usings on both levels, global usings or extern aliases, a directive
    /// before the namespace's body, a using sharing its line with other code, syntax errors, or a name that could be
    /// looked up differently afterwards (<see cref="LooksUpTheSame"/>). Comments directly above the usings move with them
    /// (not the file header).
    /// </summary>
    public static List<TextChange>? GetChanges(SemanticModel model, UsingPlacementMode mode, string indentUnit, CancellationToken cancellationToken)
    {
        var tree = model.SyntaxTree;
        var text = tree.GetText(cancellationToken);
        if (mode == UsingPlacementMode.Preserve || tree.GetRoot(cancellationToken) is not CompilationUnitSyntax root
            || root.ContainsDiagnostics || root.Externs.Count > 0 || root.AttributeLists.Count > 0
            || root.Members.Count != 1 || root.Members[0] is not BaseNamespaceDeclarationSyntax ns
            || ns.Externs.Count > 0 || ns.Members.Any(m => m is BaseNamespaceDeclarationSyntax))
        {
            return null;
        }

        var (moved, target) = mode == UsingPlacementMode.Outside ? (ns.Usings, root.Usings) : (root.Usings, ns.Usings);
        if (moved.Count == 0 || target.Count > 0 || moved.Any(u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)))
        {
            return null;
        }

        // Relative names get their full name when they move out (BRO1126's fix, so both rules agree in any order).
        var names = moved.Select(u => mode == UsingPlacementMode.Outside && QualifiedUsings.GetQualifiedName(u, model, cancellationToken) is { } qualified
            ? (u.Name, (TypeSyntax)SyntaxFactory.ParseTypeName(qualified))
            : ((NameSyntax?)null, u.NamespaceOrType)).ToList();
        return GetSyntaxChanges(root, ns, moved, names, text, mode, indentUnit) is { } changes
            && LooksUpTheSame(model, root, ns, moved, names.Select(n => n.Item2), cancellationToken)
            ? changes
            : null;
    }

    private static List<TextChange>? GetSyntaxChanges(
        CompilationUnitSyntax root,
        BaseNamespaceDeclarationSyntax ns,
        SyntaxList<UsingDirectiveSyntax> moved,
        List<(NameSyntax? Replaced, TypeSyntax Name)> names,
        SourceText text,
        UsingPlacementMode mode,
        string indentUnit)
    {
        // Where the namespace's body starts: no directive ('#if', '#pragma', ...) may sit anywhere before it.
        var bodyStart = ns is NamespaceDeclarationSyntax blockNamespace ? blockNamespace.OpenBraceToken : ((FileScopedNamespaceDeclarationSyntax)ns).SemicolonToken;
        var checkedEnd = mode == UsingPlacementMode.Outside ? moved.Last().FullSpan.End : bodyStart.FullSpan.End;
        if (root.DescendantTrivia(TextSpan.FromBounds(0, checkedEnd)).Any(t => t.IsDirective))
        {
            return null;
        }

        // The usings must have their lines to themselves (a trailing comment is fine).
        var lines = text.Lines;
        var first = moved.First();
        var last = moved.Last();
        var firstLine = lines.GetLineFromPosition(first.SpanStart).LineNumber;
        var lastLine = lines.GetLineFromPosition(last.Span.End).LineNumber;
        var indentation = Indentation(lines[firstLine]);
        if (lines[firstLine].Start + indentation.Length != first.SpanStart
            || lines.GetLineFromPosition(last.GetLastToken().GetNextToken().SpanStart).LineNumber == lastLine)
        {
            return null;
        }

        // Comment lines directly above the first using move along, unless they start the file (its header).
        var startLine = firstLine;
        while (startLine > 0 && !IsBlank(lines[startLine - 1]) && lines[startLine - 1].Start >= first.FullSpan.Start)
        {
            startLine--;
        }

        if (startLine == 0 && mode == UsingPlacementMode.Inside)
        {
            startLine = firstLine;
        }

        // The moved lines, with qualified names and their indentation replaced.
        var block = text.GetSubText(TextSpan.FromBounds(lines[startLine].Start, lines[lastLine].EndIncludingLineBreak));
        block = block.WithChanges(names.Where(n => n.Replaced is not null)
            .Select(n => new TextChange(new TextSpan(n.Replaced!.SpanStart - lines[startLine].Start, n.Replaced.Span.Length), n.Name.ToString())));
        var newIndentation = ns is NamespaceDeclarationSyntax && mode == UsingPlacementMode.Inside
            ? Indentation(lines.GetLineFromPosition(ns.SpanStart)) + indentUnit
            : string.Empty;
        var reindented = new StringBuilder();
        foreach (var line in block.Lines)
        {
            var content = block.ToString(line.SpanIncludingLineBreak);
            if (!IsBlank(line))
            {
                if (!content.StartsWith(indentation, System.StringComparison.Ordinal))
                {
                    return null;
                }

                content = newIndentation + content.Substring(indentation.Length);
            }

            reindented.Append(content);
        }

        // The removal takes the blank lines below; one stays where code above would otherwise touch what follows.
        var endLine = lastLine + 1;
        while (endLine < lines.Count && IsBlank(lines[endLine]))
        {
            endLine++;
        }

        var aboveIsOpenBrace = ns is NamespaceDeclarationSyntax braces && startLine > 0
            && lines.GetLineFromPosition(braces.OpenBraceToken.SpanStart).LineNumber == startLine - 1;
        if (endLine > lastLine + 1 && startLine > 0 && !IsBlank(lines[startLine - 1]) && !aboveIsOpenBrace)
        {
            endLine--;
        }

        var removalEnd = endLine < lines.Count ? lines[endLine].Start : text.Length;
        var changes = new List<TextChange> { new(TextSpan.FromBounds(lines[startLine].Start, removalEnd), string.Empty) };
        var lineBreak = text.ToString(TextSpan.FromBounds(lines[lastLine].End, lines[lastLine].EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            return null;
        }

        if (mode == UsingPlacementMode.Outside)
        {
            // Above the namespace and the comments directly above it, below the file header.
            var insertLine = lines.GetLineFromPosition(ns.SpanStart).LineNumber;
            while (insertLine > 0 && !IsBlank(lines[insertLine - 1]))
            {
                insertLine--;
            }

            if (insertLine == 0 && lines.GetLineFromPosition(ns.SpanStart).LineNumber > 0)
            {
                insertLine = lines.GetLineFromPosition(ns.SpanStart).LineNumber;
            }

            changes.Insert(0, new TextChange(new TextSpan(lines[insertLine].Start, 0), reindented + lineBreak));
            return changes;
        }

        // Inside: on the line below '{', or after a blank line below 'namespace X;'; a blank line before what follows.
        var headerLine = lines.GetLineFromPosition(bodyStart.SpanStart).LineNumber;
        if (headerLine + 1 >= lines.Count || lines.GetLineFromPosition(bodyStart.GetNextToken().SpanStart).LineNumber == headerLine)
        {
            return null;
        }

        var next = lines[headerLine + 1];
        var nextIsClose = ns is NamespaceDeclarationSyntax closing && lines.GetLineFromPosition(closing.CloseBraceToken.SpanStart).LineNumber == headerLine + 1;
        var inserted = (ns is FileScopedNamespaceDeclarationSyntax ? lineBreak : string.Empty)
            + reindented
            + (IsBlank(next) || nextIsClose ? string.Empty : lineBreak);
        changes.Add(new TextChange(new TextSpan(next.Start, 0), inserted));
        return changes;
    }

    /// <summary>
    /// Whether every name in the file is looked up the same with the usings moved, by C#'s lookup rules (no rebinding of
    /// the file). For namespace N (in containers C1..Cn, then the global namespace), lookup goes
    /// inside: N's members, the moved usings (U), C1..Cn's members, the global namespace's, the global usings (G);
    /// outside: N's members, C1..Cn's members, the global namespace's, U together with G.
    /// So a name found in N is the same either way; a name U imports must not be found in C1..Cn/global (unless as the
    /// same symbols), nor, when none of those has it, through G as anything U doesn't import too. Extension methods of
    /// the same name from U and from N, its containers or G could change overload resolution: not allowed. Names are
    /// compared by text (every identifier in the file, '-Attribute' added in attributes), arity ignored: conservative.
    /// The usings' own names: their first identifier must be a member of the global namespace and of no namespace of N's
    /// (inside N it would resolve there; at the top, only global members are seen).
    /// </summary>
    private static bool LooksUpTheSame(
        SemanticModel model,
        CompilationUnitSyntax root,
        BaseNamespaceDeclarationSyntax ns,
        SyntaxList<UsingDirectiveSyntax> moved,
        IEnumerable<TypeSyntax> movedNames,
        CancellationToken cancellationToken)
    {
        if (model.GetDeclaredSymbol(ns, cancellationToken) is not INamespaceSymbol declared)
        {
            return false;
        }

        // The compilation's namespaces (members from every assembly), not the source module's: from the global one down.
        var global = model.Compilation.GlobalNamespace;
        var containers = new List<INamespaceSymbol>();
        var inner = global;
        foreach (var part in declared.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)).Split('.'))
        {
            containers.Insert(0, inner);
            if (inner.GetNamespaceMembers().FirstOrDefault(n => n.Name == part.TrimStart('@')) is not { } next)
            {
                return false;
            }

            inner = next;
        }

        var namespacesOfN = containers.Where(c => !c.IsGlobalNamespace).Prepend(inner).ToList();
        foreach (var name in movedNames)
        {
            foreach (var first in FirstIdentifiers(name))
            {
                if (!global.GetMembers(first).Any() || namespacesOfN.Any(n => n.GetMembers(first).Any()))
                {
                    return false;
                }
            }
        }

        // What U imports, and what the global usings (every other import in scope at the top) do.
        var movedSpans = moved.Select(u => u.Span).ToList();
        bool IsMoved(SyntaxReference? reference) =>
            reference is not null && reference.SyntaxTree == root.SyntaxTree && movedSpans.Any(s => s.Contains(reference.Span));
        var scopes = model.GetImportScopes(ns.Members.Count > 0 ? ns.Members[0].SpanStart : ns.Span.End - 1, cancellationToken);
        var imports = scopes.SelectMany(s => s.Imports).ToList();
        var aliases = scopes.SelectMany(s => s.Aliases).ToList();
        var compilation = model.Compilation;
        var u = new Imports(compilation, imports.Where(i => IsMoved(i.DeclaringSyntaxReference)).Select(i => i.NamespaceOrType), aliases.Where(a => a.DeclaringSyntaxReferences.Any(IsMoved)));
        var g = new Imports(compilation, imports.Where(i => !IsMoved(i.DeclaringSyntaxReference)).Select(i => i.NamespaceOrType), aliases.Where(a => !a.DeclaringSyntaxReferences.Any(IsMoved)));

        var (identifiers, methodNames) = GetNames(root, TextSpan.FromBounds(moved.First().SpanStart, moved.Last().Span.End));
        foreach (var name in identifiers)
        {
            var fromU = u.Find(name);
            if (fromU.Count == 0 || inner.GetMembers(name).Any())
            {
                continue;
            }

            var fromContainers = new HashSet<ISymbol>(containers.SelectMany(c => c.GetMembers(name)).Where(m => IsVisible(compilation, m)), SymbolEqualityComparer.Default);
            if (fromContainers.Count > 0 ? !fromContainers.SetEquals(fromU) : !g.Find(name).IsSubsetOf(fromU))
            {
                return false;
            }
        }

        foreach (var name in methodNames)
        {
            var fromU = new HashSet<IMethodSymbol>(u.Extensions(name), SymbolEqualityComparer.Default);
            if (fromU.Count > 0
                && !g.Extensions(name).Concat(namespacesOfN.Append(global).SelectMany(n => ExtensionsOf(compilation, n)[name])).All(fromU.Contains))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Every identifier the file uses outside <paramref name="usings"/> (with 'XAttribute' for an attribute's 'X'), and the
    /// names that may be extension method calls: after '.', and the ones the compiler looks up itself.
    /// </summary>
    private static (HashSet<string> Identifiers, HashSet<string> MethodNames) GetNames(CompilationUnitSyntax root, TextSpan usings)
    {
        var identifiers = new HashSet<string>(System.StringComparer.Ordinal);
        var methods = new HashSet<string>(ImplicitMethodNames, System.StringComparer.Ordinal);
        foreach (var node in TreeWalk.Nodes(root))
        {
            if (usings.Contains(node.Span))
            {
                continue;
            }

            switch (node)
            {
                case SimpleNameSyntax simple:
                    identifiers.Add(simple.Identifier.ValueText);
                    break;
                case AttributeSyntax { Name: var name }:
                    identifiers.Add(name.GetLastToken().ValueText + "Attribute");
                    break;
                case MemberAccessExpressionSyntax access:
                    methods.Add(access.Name.Identifier.ValueText);
                    break;
                case MemberBindingExpressionSyntax binding:
                    methods.Add(binding.Name.Identifier.ValueText);
                    break;
            }
        }

        return (identifiers, methods);
    }

    /// <summary>The first identifier of each name in a using's target (the one looked up in scope), not after 'global::'.</summary>
    private static IEnumerable<string> FirstIdentifiers(TypeSyntax type) =>
        type.DescendantNodesAndSelf()
            .OfType<SimpleNameSyntax>()
            .Where(n => n.Parent is not QualifiedNameSyntax { Right: var right } || right != n)
            .Where(n => n.Parent is not AliasQualifiedNameSyntax)
            .Select(n => n.Identifier.ValueText);

    /// <summary>
    /// The extension methods a namespace's types (or a 'using static' type) declare, by name; cached per compilation, since
    /// every file imports the same few namespaces.
    /// </summary>
    private static ILookup<string, IMethodSymbol> ExtensionsOf(Compilation compilation, INamespaceOrTypeSymbol target) =>
        ExtensionCache.GetValue(compilation, _ => new ConcurrentDictionary<INamespaceOrTypeSymbol, ILookup<string, IMethodSymbol>>(SymbolEqualityComparer.Default))
            .GetOrAdd(target, t => (t is INamespaceSymbol ns ? ns.GetTypeMembers() : t is INamedTypeSymbol type ? ImmutableArray.Create(type) : ImmutableArray<INamedTypeSymbol>.Empty)
                .Where(type => type.MightContainExtensionMethods && IsVisible(compilation, type))
                .SelectMany(type => type.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(m => m.IsExtensionMethod && IsVisible(compilation, m))
                .ToLookup(m => m.Name));

    /// <summary>Only what the file's code could use: not another assembly's internal types (lookup skips them).</summary>
    private static bool IsVisible(Compilation compilation, ISymbol symbol) =>
        symbol is INamespaceSymbol || compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);

    private static string Indentation(TextLine line)
    {
        var text = line.ToString();
        return text.Substring(0, text.Length - text.TrimStart().Length);
    }

    private static bool IsBlank(TextLine line) => string.IsNullOrWhiteSpace(line.ToString());

    /// <summary>What a set of using directives brings into scope, by name.</summary>
    private sealed class Imports
    {
        private readonly Compilation compilation;
        private readonly List<INamespaceOrTypeSymbol> targets;
        private readonly List<IAliasSymbol> aliases;

        public Imports(Compilation compilation, IEnumerable<INamespaceOrTypeSymbol> targets, IEnumerable<IAliasSymbol> aliases)
        {
            this.compilation = compilation;
            this.targets = targets.ToList();
            this.aliases = aliases.ToList();
        }

        /// <summary>The types (of a namespace), static members and nested types (of a 'using static' type) and alias targets named <paramref name="name"/>.</summary>
        public HashSet<ISymbol> Find(string name)
        {
            var found = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var target in this.targets)
            {
                found.UnionWith(target is INamespaceSymbol ns
                    ? ns.GetTypeMembers(name).Where(t => IsVisible(this.compilation, t))
                    : target.GetMembers(name).Where(m => (m.IsStatic || m is ITypeSymbol) && IsVisible(this.compilation, m)));
            }

            found.UnionWith(this.aliases.Where(a => a.Name == name).Select(a => a.Target));
            return found;
        }

        /// <summary>The extension methods named <paramref name="name"/> of the imported namespaces' types and the 'using static' types.</summary>
        public IEnumerable<IMethodSymbol> Extensions(string name) => this.targets.SelectMany(t => ExtensionsOf(this.compilation, t)[name]);
    }
}
