using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// What the SDK's naming rules (IDE1006) say about a rename every naming rule (BRO1301-BRO1314) is about to report, checked
/// last, when everything else says yes:
/// <list type="bullet">
/// <item>where IDE1006 is suppressed ('#pragma warning disable IDE1006' up to its restore, or [SuppressMessage] for
/// IDE1006 on the symbol, a containing member or type, or a global one targeting them), names stay (owner's choice
/// 2026-10-14: MoreLINQ's Lookup.cs, copied from dotnet/runtime, starts with the pragma);</item>
/// <item>so do names where ReSharper's InconsistentNaming inspection is suppressed ('// ReSharper disable once
/// InconsistentNaming' above the declaration, or 'disable' up to its 'restore'): the author said the name is on purpose
/// (owner's choice 2026-10-23, trial 49: Cocona's '[Argument] string ArgName0', which it binds by name);</item>
/// <item>the first SDK naming rule that covers the symbol (Roslyn's order), when enforced, must accept the new name:
/// Altinn's rule for private properties asked for '_camelCase' and BRO1309's '_format' -> 'Format' broke the build with
/// IDE1006 at error (trial 20).</item>
/// </list>
/// </summary>
internal static class NamingRules
{
    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, List<Rule>> RuleCache = new();
    private static readonly ConditionalWeakTable<SyntaxTree, List<(int Position, bool Disabled)>> PragmaCache = new();
    private static readonly ConditionalWeakTable<Compilation, List<(string? Scope, string? Target)>> GlobalCache = new();
    private static readonly ConditionalWeakTable<SyntaxTree, List<(int Position, bool Disabled)>> ReSharperCache = new();
    private static readonly Regex ReSharperComment = new(@"^//\s*ReSharper\s+(?<verb>disable|restore)(?<once>\s+once)?\s+(?<ids>.+?)\s*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Whether a naming rule may rename <paramref name="symbol"/> (declared at <paramref name="location"/>) to
    /// <paramref name="newName"/>; null checks only the suppressions (tuple elements, which IDE1006 doesn't name).
    /// </summary>
    public static bool AllowsRename(Compilation compilation, AnalyzerConfigOptions options, Location location, ISymbol symbol, string? newName, CancellationToken cancellationToken)
    {
        if (location.SourceTree is not { } tree
            || IsDisabledByPragma(tree, location.SourceSpan.Start, cancellationToken)
            || IsDisabledByReSharper(tree, location.SourceSpan.Start, cancellationToken)
            || IsSuppressedByAttribute(compilation, symbol))
        {
            return false;
        }

        if (newName is null || GetKind(symbol) is not { } kind)
        {
            return true;
        }

        var access = GetAccessibility(symbol);
        var rule = RuleCache.GetValue(options, Read).FirstOrDefault(r => r.AppliesTo(kind, access, symbol));
        return rule is null
            || !IsEnforced(rule.Severity, Severities.Configured(compilation.Options, tree, "IDE1006", cancellationToken, options, "Style"))
            || rule.Accepts(newName);
    }

    /// <summary>Whether '#pragma warning disable IDE1006' (or a bare 'disable') is in effect at the position.</summary>
    internal static bool IsDisabledByPragma(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var disabled = false;
        foreach (var (start, isDisabled) in PragmaCache.GetValue(tree, t => ReadPragmas(t, cancellationToken)))
        {
            if (start >= position)
            {
                break;
            }

            disabled = isDisabled;
        }

        return disabled;
    }

    /// <summary>
    /// Whether ReSharper's InconsistentNaming inspection is suppressed at the position: '// ReSharper disable once
    /// InconsistentNaming' right above the declaration, statement or member the name is in (the nearest one), or a
    /// '// ReSharper disable InconsistentNaming' before it that no 'restore' ended.
    /// </summary>
    internal static bool IsDisabledByReSharper(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var comments = ReSharperCache.GetValue(tree, t => ReadReSharperComments(t, cancellationToken));
        if (comments.Count == 0)
        {
            return false;
        }

        var disabled = false;
        foreach (var (start, isDisabled) in comments)
        {
            if (start >= position)
            {
                break;
            }

            disabled = isDisabled;
        }

        if (disabled)
        {
            return true;
        }

        for (var node = tree.GetRoot(cancellationToken).FindToken(position).Parent; node is not null; node = node.Parent)
        {
            if (node.GetLeadingTrivia().Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) && IsInconsistentNaming(t.ToString(), once: true) == true))
            {
                return true;
            }

            if (node is MemberDeclarationSyntax or StatementSyntax)
            {
                return false;
            }
        }

        return false;
    }

    // The 'disable'/'restore' comments without 'once', in order.
    private static List<(int Position, bool Disabled)> ReadReSharperComments(SyntaxTree tree, CancellationToken cancellationToken)
    {
        var result = new List<(int, bool)>();
        foreach (var trivia in TreeWalk.Trivia(tree.GetRoot(cancellationToken)))
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && IsInconsistentNaming(trivia.ToString(), once: false) is { } disabled)
            {
                result.Add((trivia.SpanStart, disabled));
            }
        }

        return result;
    }

    // For a ReSharper comment naming InconsistentNaming ('disable once A, InconsistentNaming'), with or without 'once' as
    // asked: true for 'disable', false for 'restore'; null for any other comment.
    private static bool? IsInconsistentNaming(string comment, bool once)
    {
        var match = ReSharperComment.Match(comment);
        return match.Success && match.Groups["once"].Success == once
            && match.Groups["ids"].Value.Split(',').Any(id => id.Trim() == "InconsistentNaming")
            ? match.Groups["verb"].Value == "disable"
            : null;
    }

    private static List<(int Position, bool Disabled)> ReadPragmas(SyntaxTree tree, CancellationToken cancellationToken)
    {
        var result = new List<(int, bool)>();
        var root = tree.GetRoot(cancellationToken);
        if (!root.ContainsDirectives)
        {
            return result;
        }

        foreach (var trivia in TreeWalk.Trivia(root))
        {
            if (trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                && trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax { IsActive: true } pragma
                && (pragma.ErrorCodes.Count == 0 || pragma.ErrorCodes.Any(c => (c is IdentifierNameSyntax name ? name.Identifier.ValueText : c.ToString()) == "IDE1006")))
            {
                result.Add((trivia.SpanStart, pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword)));
            }
        }

        return result;
    }

    /// <summary>
    /// [SuppressMessage(..., "IDE1006...")] on the symbol or anything containing it, or a global one without a target (the
    /// whole compilation) or targeting one of them ('~T:Ns.Type'; a namespace with Scope = "namespaceanddescendants").
    /// </summary>
    private static bool IsSuppressedByAttribute(Compilation compilation, ISymbol symbol)
    {
        for (var current = symbol; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.GetAttributes().Any(a => IsIde1006(a)))
            {
                return true;
            }
        }

        var global = GlobalCache.GetValue(compilation, c => c.Assembly.GetAttributes().Concat(c.SourceModule.GetAttributes())
            .Where(IsIde1006)
            .Select(a => (Named(a, "Scope")?.ToLowerInvariant(), Named(a, "Target")?.TrimStart('~')))
            .ToList());
        if (global.Count == 0)
        {
            return false;
        }

        if (global.Any(g => g.Target is null && g.Scope is null or "module"))
        {
            return true;
        }

        for (var current = symbol; current is not null and not IAssemblySymbol and not IModuleSymbol; current = current.ContainingSymbol)
        {
            if (current.GetDocumentationCommentId() is { } id
                && global.Any(g => g.Target == id && (current is not INamespaceSymbol || ReferenceEquals(current, symbol) || g.Scope == "namespaceanddescendants")))
            {
                return true;
            }
        }

        return false;

        static string? Named(AttributeData attribute, string name) =>
            attribute.NamedArguments.FirstOrDefault(n => n.Key == name).Value.Value as string;
    }

    private static bool IsIde1006(AttributeData attribute) =>
        attribute.AttributeClass?.Name is "SuppressMessageAttribute" or "UnconditionalSuppressMessageAttribute"
        && attribute.ConstructorArguments.Length == 2
        && attribute.ConstructorArguments[1].Value is string id
        && id.Split(':')[0].Trim() == "IDE1006";

    // Like FieldNames' rules: IDE1006's own severity when set, else the rule's, must be suggestion, warning or error.
    private static bool IsEnforced(string severity, ReportDiagnostic? ide1006) => severity != "none"
        && (ide1006 is { } set and not ReportDiagnostic.Default
            ? set is ReportDiagnostic.Info or ReportDiagnostic.Warn or ReportDiagnostic.Error
            : severity is "suggestion" or "warning" or "error");

    /// <summary>The symbol's kind as applicable_kinds names it, or null for those no naming rule covers (range variables).</summary>
    private static string? GetKind(ISymbol symbol) => symbol switch
    {
        INamespaceSymbol => "namespace",
        INamedTypeSymbol type => type.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            _ => null,
        },
        IMethodSymbol { MethodKind: MethodKind.LocalFunction } => "local_function",
        IMethodSymbol { MethodKind: MethodKind.Ordinary } => "method",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        IParameterSymbol => "parameter",
        ITypeParameterSymbol => "type_parameter",
        ILocalSymbol => "local",
        _ => null,
    };

    /// <summary>Roslyn's: a parameter or type parameter has its member's; locals and what's inside a local function or lambda 'local'.</summary>
    private static string GetAccessibility(ISymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingSymbol)
        {
            switch (current)
            {
                case IParameterSymbol or ITypeParameterSymbol:
                    continue;

                case ILocalSymbol or IRangeVariableSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction }:
                    return "local";
            }

            return current.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected_internal",
                Accessibility.ProtectedAndInternal => "private_protected",
                _ => "local",
            };
        }

        return "local";
    }

    /// <summary>The rules with a symbols group, a style and a severity, in Roslyn's order (see FieldNames' GetNonPrivateStyles).</summary>
    private static List<Rule> Read(AnalyzerConfigOptions options)
    {
        string? Get(string key) => options.TryGetValue(key, out var v) ? v.Split(':')[0].Trim() : null;
        static string[]? List(string? value) =>
            value is null || value.Split(',').Any(p => p.Trim() == "*")
            ? null
            : value.Split(',').Select(p => p.Trim() switch { "friend" => "internal", "protected_friend" => "protected_internal", var x => x }).Where(p => p.Length > 0).ToArray();

        var rules = new List<Rule>();
        foreach (var key in options.Keys)
        {
            if (!key.StartsWith("dotnet_naming_rule.", System.StringComparison.Ordinal) || !key.EndsWith(".symbols", System.StringComparison.Ordinal))
            {
                continue;
            }

            var name = key.Substring("dotnet_naming_rule.".Length, key.Length - "dotnet_naming_rule.".Length - ".symbols".Length);
            var symbols = Get(key);
            var style = Get($"dotnet_naming_rule.{name}.style");
            var severity = Get($"dotnet_naming_rule.{name}.severity");

            // Roslyn ignores a rule without these, and a style without a capitalization.
            if (symbols is null || style is null || severity is null || Get($"dotnet_naming_style.{style}.capitalization") is null)
            {
                continue;
            }

            rules.Add(new Rule(
                int.TryParse(Get($"dotnet_naming_rule.{name}.priority"), out var priority) ? priority : 0,
                name,
                severity,
                List(Get($"dotnet_naming_symbols.{symbols}.applicable_kinds")),
                List(Get($"dotnet_naming_symbols.{symbols}.applicable_accessibilities")),
                List(Get($"dotnet_naming_symbols.{symbols}.required_modifiers")) ?? new string[0],
                Get($"dotnet_naming_style.{style}.required_prefix") ?? string.Empty,
                Get($"dotnet_naming_style.{style}.required_suffix") ?? string.Empty,
                Get($"dotnet_naming_style.{style}.word_separator") ?? string.Empty,
                Get($"dotnet_naming_style.{style}.capitalization")));
        }

        return rules
            .OrderBy(r => r.Priority)
            .ThenBy(r => r, Comparer<Rule>.Create((a, b) => Order(MatchesFewer(a, b), MatchesFewer(b, a))))
            .ThenBy(r => r, Comparer<Rule>.Create((a, b) => Subset(a.Accessibilities, b.Accessibilities)))
            .ThenBy(r => r, Comparer<Rule>.Create((a, b) => Subset(a.Kinds, b.Kinds)))
            .ThenBy(r => r.Name, System.StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Name, System.StringComparer.Ordinal)
            .ToList();

        // -1 when x is a proper subset of y (null: everything), 1 the other way round, else 0.
        static int Subset(string[]? x, string[]? y)
        {
            var xInY = y is null || (x is not null && x.All(y.Contains));
            var yInX = x is null || (y is not null && y.All(x.Contains));
            return Order(xInY, yInX);
        }

        static int Order(bool xInY, bool yInX) => xInY == yInX ? 0 : xInY ? -1 : 1;

        // Roslyn's NamingRuleModifierListComparer: x requires every modifier y does, where 'const' implies 'static' and 'readonly'.
        static bool MatchesFewer(Rule x, Rule y) =>
            y.Modifiers.All(m => x.Modifiers.Contains(m) || (m is "static" or "shared" or "readonly" && x.Modifiers.Contains("const")));
    }

    private sealed class Rule
    {
        private readonly string prefix;
        private readonly string suffix;
        private readonly string separator;
        private readonly string? capitalization;

        public Rule(int priority, string name, string severity, string[]? kinds, string[]? accessibilities, string[] modifiers, string prefix, string suffix, string separator, string? capitalization)
        {
            Priority = priority;
            Name = name;
            Severity = severity;
            Kinds = kinds;
            Accessibilities = accessibilities;
            Modifiers = modifiers;
            this.prefix = prefix;
            this.suffix = suffix;
            this.separator = separator;
            this.capitalization = capitalization;
        }

        public int Priority { get; }

        public string Name { get; }

        public string Severity { get; }

        public string[]? Kinds { get; }

        public string[]? Accessibilities { get; }

        public string[] Modifiers { get; }

        public bool AppliesTo(string kind, string access, ISymbol symbol) =>
            (Kinds is null || Kinds.Contains(kind))
            && (Accessibilities is null || Accessibilities.Contains(access))
            && Modifiers.All(m => m switch
            {
                "abstract" or "must_inherit" => symbol.IsAbstract,
                "async" => symbol is IMethodSymbol { IsAsync: true },
                "const" => symbol is IFieldSymbol { IsConst: true } or ILocalSymbol { IsConst: true },
                "readonly" => symbol is IFieldSymbol { IsReadOnly: true } or IFieldSymbol { IsConst: true },
                "static" or "shared" => symbol.IsStatic || symbol is IFieldSymbol { IsConst: true },
                _ => false,
            });

        /// <summary>Roslyn's NamingStyle.IsNameCompliant: prefix, suffix, and each word's casing (an uncased first character passes).</summary>
        public bool Accepts(string name)
        {
            if (!name.StartsWith(prefix, System.StringComparison.Ordinal) || !name.EndsWith(suffix, System.StringComparison.Ordinal) || name.Length < prefix.Length + suffix.Length)
            {
                return false;
            }

            var core = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            var words = separator.Length == 0 ? new[] { core } : core.Split(new[] { separator }, System.StringSplitOptions.None);
            static bool Upper(char c) => !char.IsLetter(c) || char.IsUpper(c);
            static bool Lower(char c) => !char.IsLetter(c) || char.IsLower(c);
            return capitalization switch
            {
                "pascal_case" => words.All(w => w.Length == 0 || Upper(w[0])),
                "camel_case" => words.Select((w, i) => w.Length == 0 || (i == 0 ? Lower(w[0]) : Upper(w[0]))).All(ok => ok),
                "first_word_upper" => words.Select((w, i) => i == 0 ? w.All(Upper) : w.All(Lower)).All(ok => ok),
                "all_upper" => core.All(Upper),
                "all_lower" => core.All(Lower),
                _ => false,
            };
        }
    }
}
