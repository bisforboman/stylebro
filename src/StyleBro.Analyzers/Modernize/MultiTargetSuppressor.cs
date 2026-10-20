using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Modernize;

/// <summary>
/// The multi-target guard (docs/modernizing.md): hides a diagnostic of an SDK rule whose fix writes a newer API (tier C of
/// 'bro init --modernize') when the project targets several frameworks and one of them lacks that API. The
/// SDK rules check the API per target framework, so they report in the newer framework's compilation and 'dotnet format'
/// writes the fix into the file every framework shares. The package passes the framework list as
/// <c>build_property.StyleBroTargetFrameworks</c> (comma-separated: ';' starts a comment in the generated editorconfig).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultiTargetSuppressor : DiagnosticSuppressor
{
    /// <summary>The compiler-visible property with the project's target frameworks.</summary>
    public const string Property = "build_property.StyleBroTargetFrameworks";

    /// <summary>
    /// Rule -> the first .NET (Core) version and .NET Standard version (null: none) with every API the rule's fix can
    /// write. .NET Framework has none of them. Maintenance: add a row when tier C (modernize-rules.editorconfig) gets a rule
    /// (MigrationTests checks both lists match).
    /// </summary>
    // ponytail: hand-kept table; a minimum that's too high only hides a fix, one that's too low lets a fix break a build.
    internal static readonly ImmutableDictionary<string, (Version Core, Version? Standard)> Minimums = new Dictionary<string, (Version, Version?)>
    {
        ["CA1510"] = (new(6, 0), null),               // ArgumentNullException.ThrowIfNull
        ["CA1511"] = (new(7, 0), null),               // ArgumentException.ThrowIfNullOrEmpty
        ["CA1512"] = (new(8, 0), null),               // ArgumentOutOfRangeException.ThrowIfNegative, ...
        ["CA1513"] = (new(7, 0), null),               // ObjectDisposedException.ThrowIf
        ["CA1847"] = (new(2, 1), new(2, 1)),          // string.Contains(char)
        ["CA1850"] = (new(6, 0), null),               // static HashData (SHA*/MD5 .NET 5, HMAC* .NET 6)
        ["CA1864"] = (new(2, 0), new(2, 1)),          // Dictionary.TryAdd
        ["CA1865"] = (new(2, 1), new(2, 1)),          // StartsWith(char), EndsWith(char), IndexOf(char, StringComparison)
        ["CA1872"] = (new(9, 0), null),               // Convert.ToHexString (.NET 5), ToHexStringLower (.NET 9)
        ["CA2249"] = (new(2, 1), new(2, 1)),          // string.Contains(string, StringComparison)
        ["CA2263"] = (new(5, 0), null),               // generic overloads: Enum.Parse<T> (Core 2.0), Enum.GetValues<T> (.NET 5), ...
        ["IDE0056"] = (new(3, 0), new(2, 1)),         // System.Index
        ["IDE0057"] = (new(3, 0), new(2, 1)),         // System.Range
        ["IDE0330"] = (new(9, 0), null),              // System.Threading.Lock
    }.ToImmutableDictionary();

    /// <summary>The SDK's rule for built-in type names ('Int32' -> 'int', 'IntPtr' -> 'nint').</summary>
    internal const string TypeNamesId = "IDE0049";

    /// <summary>
    /// The extension of the list 'bro format' passes its runs as an additional file: one line per file another project
    /// compiles with an older C#, '&lt;lowest version&gt;\t&lt;full path&gt;' (trial 35: a file linked into a net8.0 project and a
    /// netstandard2.0 one; each project's suppressor sees only its own frameworks).
    /// </summary>
    public const string OldCSharpExtension = ".oldcsharp";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = Minimums.Keys.ToImmutableDictionary(
        id => id,
        id => new SuppressionDescriptor("MULTITARGET_" + id, id, "A target framework of this project lacks the API the fix writes (StyleBro's multi-target guard)"));

    private static readonly SuppressionDescriptor TypeNames = new(
        "MULTITARGET_" + TypeNamesId, TypeNamesId, "A target framework of this project compiles with a C# version too old for what the fix writes (StyleBro's multi-target guard)");

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.Append(TypeNames).ToImmutableArray();

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        // A single target: the rule itself checks that the API exists (AllHave).
        var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        Dictionary<string, Version>? oldCSharp = null;
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (Descriptors.TryGetValue(diagnostic.Id, out var descriptor) && !AllHave(options, Minimums[diagnostic.Id]))
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
            else if (diagnostic.Id == TypeNamesId && diagnostic.Location.SourceTree is { } tree
                && TypeNameFixNeeds(tree, diagnostic.Location.SourceSpan, context.CancellationToken) is { } core
                && (!AllFrameworksDefaultTo(options, core)
                    || ((oldCSharp ??= ReadOldCSharp(context)).TryGetValue(NormalizePath(tree.FilePath), out var lowest) && lowest < CSharpFor(core))))
            {
                context.ReportSuppression(Suppression.Create(TypeNames, diagnostic));
            }
        }
    }

    /// <summary>
    /// The project's target frameworks from <see cref="Property"/>; empty without the package's build targets (a project
    /// that references StyleBro's DLLs directly), so callers can't tell a single target from an unknown one.
    /// </summary>
    internal static List<string> GetFrameworks(AnalyzerConfigOptions options) =>
        options.TryGetValue(Property, out var value)
            ? value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).Where(f => f.Length > 0).ToList()
            : new List<string>();

    /// <summary>
    /// Whether every target framework of the project reaches the minimum; true for a single target (the compilation itself
    /// shows what it has) or an unknown list. For StyleBro's own fixes whose result binds only where an API exists (trial
    /// 21: BRO1151's params collections), since 'bro format' and 'dotnet format' fix the file every framework
    /// shares from the newest framework's findings.
    /// </summary>
    internal static bool AllHave(AnalyzerConfigOptions options, (Version Core, Version? Standard) minimum)
    {
        var frameworks = GetFrameworks(options);
        return frameworks.Count < 2 || frameworks.All(f => Has(f, minimum));
    }

    /// <summary>
    /// The .NET version whose default C# an IDE0049 fix at <paramref name="span"/> needs, or null when any C# takes it: a
    /// keyword in a using alias needs C# 12 (.NET 8's default), 'nint'/'nuint' for IntPtr/UIntPtr C# 9 (.NET 5's).
    /// </summary>
    internal static Version? TypeNameFixNeeds(SyntaxTree tree, Microsoft.CodeAnalysis.Text.TextSpan span, System.Threading.CancellationToken cancellationToken)
    {
        var node = tree.GetRoot(cancellationToken).FindNode(span);
        if (node.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().Any(u => u.Alias is not null))
        {
            return new Version(8, 0);
        }

        var name = tree.GetText(cancellationToken).ToString(span).Trim();
        name = name.Substring(name.LastIndexOf('.') + 1);
        return name is "IntPtr" or "UIntPtr" ? new Version(5, 0) : null;
    }

    /// <summary>The C# version .NET <paramref name="core"/> defaults to (<see cref="TypeNameFixNeeds"/>'s two: .NET 5 C# 9, .NET 8 C# 12).</summary>
    internal static Version CSharpFor(Version core) => new(core.Major + 4, 0);

    /// <summary>The files and lowest C# versions 'bro format' listed (<see cref="OldCSharpExtension"/>); empty without the list.</summary>
    internal static Dictionary<string, Version> ReadOldCSharp(SuppressionAnalysisContext context)
    {
        var files = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in context.Options.AdditionalFiles.Where(f => f.Path.EndsWith(OldCSharpExtension, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var line in list.GetText(context.CancellationToken)?.Lines.Select(l => l.ToString()) ?? Enumerable.Empty<string>())
            {
                var tab = line.IndexOf('\t');
                if (tab > 0 && Version.TryParse(line.Substring(0, tab), out var version))
                {
                    files[NormalizePath(line.Substring(tab + 1))] = version;
                }
            }
        }

        return files;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Whether every copy of a multi-targeted project's files compiles with at least the C# version .NET <paramref name="core"/>
    /// defaults to: every framework reaches that .NET (no .NET Standard or .NET Framework default is that new), or the
    /// project sets LangVersion itself (then every framework uses it, and the compilation's own version decides; a
    /// LangVersion that differs per framework isn't detected). True for a single target or an unknown list.
    /// </summary>
    internal static bool AllFrameworksDefaultTo(AnalyzerConfigOptions global, Version core)
    {
        var frameworks = GetFrameworks(global);
        if (frameworks.Count < 2 || frameworks.All(f => Has(f, (core, null))))
        {
            return true;
        }

        // The SDK sets LangVersion to MaxSupportedLangVersion when the project doesn't set it.
        global.TryGetValue(Readability.EmptyRecordBodies.LangVersionProperty, out var langVersion);
        global.TryGetValue(Readability.EmptyRecordBodies.MaxSupportedLangVersionProperty, out var defaultVersion);
        return !string.IsNullOrWhiteSpace(langVersion) && !string.Equals(langVersion!.Trim(), defaultVersion?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether a target framework (net48, netstandard2.1, netcoreapp3.1, net8.0-windows, ...) reaches the minimum.</summary>
    internal static bool Has(string framework, (Version Core, Version? Standard) minimum)
    {
        var name = framework.ToLowerInvariant();
        var dash = name.IndexOf('-');
        if (dash >= 0)
        {
            name = name.Substring(0, dash);
        }

        if (name.StartsWith("netcoreapp", StringComparison.Ordinal))
        {
            return Version.TryParse(name.Substring(10), out var core) && core >= minimum.Core;
        }

        if (name.StartsWith("netstandard", StringComparison.Ordinal))
        {
            return minimum.Standard is { } standard && Version.TryParse(name.Substring(11), out var version) && version >= standard;
        }

        // net5.0 and later have a dot, so .NET Framework (net48) doesn't parse. Anything else is unknown: treated as lacking it.
        return name.StartsWith("net", StringComparison.Ordinal)
            && Version.TryParse(name.Substring(3), out var net) && net.Major >= 5 && net >= minimum.Core;
    }
}
