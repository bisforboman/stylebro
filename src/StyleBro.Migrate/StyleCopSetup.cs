using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace StyleBro.Migrate;

/// <summary>Severities in the order of strictness, as .editorconfig spells them.</summary>
internal enum Severity
{
    None,
    Silent,
    Suggestion,
    Warning,
    Error,
}

/// <summary>
/// A part of the repository with its own StyleCop severities: a section of a sub-directory's .editorconfig, or a
/// path-specific section of the root one (like '[tests/**.cs]'). <see cref="Severities"/> holds only what it sets.
/// </summary>
internal sealed record Scope(string File, string Section, IReadOnlyDictionary<string, Severity> Severities);

/// <summary>
/// A repository's StyleCop setup: every StyleCop rule's effective severity, and the settings in stylecop.json.
/// The base severities come from StyleCop's defaults, overridden by .ruleset files, then .globalconfig files, then the
/// root .editorconfig's sections for all C# files (later sources win, like in the compiler). When several rulesets or
/// global configs disagree (one per project type), the strictest wins: the generated settings must keep everything
/// StyleCop enforced in production code. Sub-directory .editorconfig files and path-specific sections are
/// <see cref="Scopes"/>, translated in place.
/// </summary>
internal sealed class StyleCopSetup
{
    /// <summary>The key ReadConfig files a 'dotnet_analyzer_diagnostic.severity' (every rule) under, next to the categories.</summary>
    private const string AllRules = "*";

    private static readonly Regex DiagnosticKey = new(@"^dotnet_diagnostic\.(S[AX]\d{4}\w*)\.severity$", RegexOptions.IgnoreCase);
    private static readonly Regex PackageVersion = new(@"Include=""StyleCop\.Analyzers(?:\.Unstable)?""[^>]*?Version=""([^""]+)""|Include=""StyleCop\.Analyzers(?:\.Unstable)?""[^>]*>\s*<Version>([^<]+)</Version>", RegexOptions.IgnoreCase);
    private static readonly Regex DocumentationFile = new(@"<GenerateDocumentationFile>\s*true\s*<|<DocumentationFile>", RegexOptions.IgnoreCase);
    private static readonly Regex CategoryKey = new(@"^dotnet_analyzer_diagnostic\.category-StyleCop\.CSharp\.(\w+)\.severity$", RegexOptions.IgnoreCase);

    /// <summary>What an MSBuild file points to: an import, a ruleset, an additional file (stylecop.json).</summary>
    private static readonly Regex Reference = new(
        @"<Import\b[^>]*?\bProject\s*=\s*""(?<import>[^""]+)""|<CodeAnalysisRuleSet\b(?:[^>]*?\bCondition\s*=\s*""(?<condition>[^""]*)"")?[^>]*>(?<ruleset>[^<]+)</CodeAnalysisRuleSet>|<AdditionalFiles\b[^>]*?\bInclude\s*=\s*""(?<additional>[^""]+)""",
        RegexOptions.IgnoreCase);

    /// <summary>A reference that runs StyleCop in a project: a (global) package reference or an analyzer DLL.</summary>
    private static readonly Regex StyleCopReference = new(@"<(?:Global)?PackageReference\b[^>]*?\bInclude\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""|<Analyzer\b[^>]*?\bInclude\s*=\s*""[^""]*StyleCop\.Analyzers", RegexOptions.IgnoreCase);

    /// <summary>A project that opts out of the StyleCop reference it inherits ('&lt;PackageReference Remove="StyleCop.Analyzers" /&gt;').</summary>
    private static readonly Regex StyleCopRemove = new(@"<PackageReference\b[^>]*?\bRemove\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""", RegexOptions.IgnoreCase);

    /// <summary>An item or Copy task taking an .editorconfig (not one the compiler reads as a config file).</summary>
    private static readonly Regex CopiesEditorConfig = new(@"<(?!EditorConfigFiles\b|GlobalAnalyzerConfigFiles\b)[\w.]+\s[^>]*\b(?:Include|SourceFiles)\s*=\s*""[^""]*\.editorconfig""", RegexOptions.IgnoreCase);

    public StyleCopSetup(IReadOnlyDictionary<string, Severity> severities, JsonElement? settings, IReadOnlyList<string> sources, IReadOnlyList<Scope> scopes, string? version = null, bool documentationParsed = true)
    {
        Version = version;
        DocumentationParsed = documentationParsed;
        Severities = severities;
        Settings = settings;
        Sources = sources;
        Scopes = scopes;
    }

    /// <summary>Gets styleCop's rules: id, category, default severity and title, from the inventory of the DLLs.</summary>
    public static IReadOnlyList<(string Id, string Category, Severity Default, string Title)> Rules { get; } = LoadRules();

    /// <summary>Gets every StyleCop rule's effective severity.</summary>
    public IReadOnlyDictionary<string, Severity> Severities { get; }

    /// <summary>Gets the 'settings' object of stylecop.json, or null when there's none.</summary>
    public JsonElement? Settings { get; }

    /// <summary>Gets the files the setup was read from, for the report.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>Gets the StyleCop.Analyzers version the repository references, or null when none was found.</summary>
    public string? Version { get; }

    /// <summary>
    /// Gets a value indicating whether some project generates a documentation file. Without one the compiler doesn't parse XML documentation
    /// in the build, and StyleCop's rules that read it stay silent (SA0001), while 'dotnet format' does parse it.
    /// </summary>
    public bool DocumentationParsed { get; }

    /// <summary>Gets parts of the repository with their own severities.</summary>
    public IReadOnlyList<Scope> Scopes { get; }

    /// <summary>
    /// Gets the MSBuild file (relative path) that copies an .editorconfig, likely over the root one on every build
    /// (SixLabors' shared infrastructure does): the generated block would be lost. Null when none does.
    /// </summary>
    public string? EditorConfigCopiedBy { get; init; }

    /// <summary>
    /// Gets the folders (relative, '/'-separated) StyleCop runs in, when some projects reference it and others don't: the
    /// settings apply only there. Null when every project runs it, or none does (the settings then apply everywhere).
    /// </summary>
    public IReadOnlyList<string>? Folders { get; init; }

    /// <summary>Gets the folders with projects that don't run StyleCop (only when <see cref="Folders"/> isn't null).</summary>
    public IReadOnlyList<string> FoldersWithout { get; init; } = [];

    /// <summary>Gets the MSBuild files (relative) that reference StyleCop.Analyzers.</summary>
    public IReadOnlyList<string> ReferencedIn { get; init; } = [];

    /// <summary>Gets the MSBuild files (relative) that remove the StyleCop.Analyzers reference they inherit.</summary>
    public IReadOnlyList<string> RemovedIn { get; init; } = [];

    public static StyleCopSetup Read(string root)
    {
        var files = EnumerateFiles(root).ToList();
        var references = FollowReferences(root, files);
        var sources = new List<string>();
        var scopes = new List<Scope>();

        // Rules added in StyleCop 1.2 (SA1651, SA1141, ...) don't exist in 1.1.x: off, whatever the configs say.
        var msbuild = files.Where(IsMSBuild).Concat(references.MSBuild).Select(f => (File: f, Text: File.ReadAllText(f))).ToList();
        var documentationParsed = msbuild.Any(m => DocumentationFile.IsMatch(m.Text));
        var version = msbuild.Select(m => PackageVersion.Match(m.Text))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value.Trim())
            .OrderBy(v => v, StringComparer.Ordinal)
            .FirstOrDefault();
        var old = version is not null && (version.StartsWith("1.1.", StringComparison.Ordinal) || version.StartsWith("1.0.", StringComparison.Ordinal))
            ? LoadIds("StyleCopRules-1.1.118.csv")
            : null;

        // Rulesets: the repository's own and the ones its MSBuild files select (also in a submodule), each with the
        // rulesets it includes. A ruleset that is only included is read as part of the including one, whose overrides count.
        var folderRulesets = FolderRulesets(files, root);
        var rootFolder = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var rootRuleset = folderRulesets.FirstOrDefault(f => string.Equals(f.Folder, rootFolder, StringComparison.OrdinalIgnoreCase)).Ruleset;
        var selected = new HashSet<string>(references.Rulesets.Concat(folderRulesets.Select(f => f.Ruleset)), StringComparer.OrdinalIgnoreCase);
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var read = files.Where(f => Kind(f) == "ruleset").Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(f => (File: f, Rules: ReadRuleset(f, included)))
            .ToList();
        var rulesets = new List<Dictionary<string, Severity>>();
        foreach (var (file, rules) in read.Where(r => r.Rules.Count > 0 && (selected.Contains(r.File) || !included.Contains(r.File))))
        {
            rulesets.Add(rules);
            sources.Add(Path.GetRelativePath(root, file));
        }

        var globalConfigs = new List<(Dictionary<string, Severity> Severities, HashSet<string> Bulk)>();
        foreach (var file in files.Where(f => Kind(f) == "globalconfig"))
        {
            var found = ReadConfig(file, global: true);
            if (found.Count > 0)
            {
                globalConfigs.Add((found[0].Severities, found[0].Bulk));
                sources.Add(Path.GetRelativePath(root, file));
            }
        }

        // .editorconfig: the root file's sections for all C# files are the base; everything else is a scope.
        var rootSections = new List<(Dictionary<string, Severity> Severities, HashSet<string> Bulk)>();
        foreach (var file in files.Where(f => Kind(f) == "editorconfig"))
        {
            var isRoot = string.Equals(Path.GetDirectoryName(file), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
            var sections = ReadConfig(file, global: false);
            if (sections.Count == 0)
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, file);
            sources.Add(relative);
            foreach (var section in sections)
            {
                if (isRoot && AppliesToCSharp(section.Section))
                {
                    rootSections.Add((section.Severities, section.Bulk));
                }
                else
                {
                    scopes.Add(new Scope(relative, section.Section, section.Severities));
                }
            }
        }

        // Rulesets, then global configs, then the root .editorconfig. Several files of one kind (one per project type):
        // the strictest wins, a file that doesn't set a rule counting with the value before it (StyleCop's default, or the
        // rulesets' result), so a test ruleset turning a rule off doesn't turn it off for production code. A bulk entry
        // (category, every rule) doesn't override a rule's own entry anywhere, like in the compiler (SixLabors: rulesets
        // turn SA1413 off, the .editorconfig sets 'dotnet_analyzer_diagnostic.severity = warning').
        var specific = new HashSet<string>(
            read.SelectMany(r => r.Rules.Keys)
                .Concat(globalConfigs.Concat(rootSections).SelectMany(c => c.Severities.Keys.Where(id => !c.Bulk.Contains(id)))),
            StringComparer.Ordinal);
        Dictionary<string, Severity> WithoutBulkOverrides((Dictionary<string, Severity> Severities, HashSet<string> Bulk) config) =>
            config.Severities.Where(p => !config.Bulk.Contains(p.Key) || !specific.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        var globalInEffect = globalConfigs.Select(WithoutBulkOverrides).ToList();
        var rootInEffect = rootSections.Select(WithoutBulkOverrides).ToList();
        Dictionary<string, Severity> Effective(IReadOnlyList<Dictionary<string, Severity>> inEffect)
        {
            var result = Rules.ToDictionary(r => r.Id, r => r.Default);
            OverlayStrictest(result, inEffect);
            OverlayStrictest(result, globalInEffect);
            rootInEffect.ForEach(section => Overlay(result, section));
            foreach (var id in old is null ? [] : result.Keys.Where(id => !old.Contains(id)).ToList())
            {
                result[id] = Severity.None;
            }

            return result;
        }

        // When only some folders' props select a ruleset, a project elsewhere (without one of its own) gets StyleCop's
        // defaults: they count among the rulesets, else a test folder's ruleset would turn its rules off everywhere.
        var scoped = folderRulesets.Where(f => f.Ruleset != rootRuleset).Select(f => f.Folder + Path.DirectorySeparatorChar).ToList();
        if (rulesets.Count > 0 && rootRuleset is null && files.Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            && !scoped.Any(s => f.StartsWith(s, StringComparison.OrdinalIgnoreCase))
            && !File.ReadAllText(f).Contains("<CodeAnalysisRuleSet", StringComparison.OrdinalIgnoreCase)))
        {
            rulesets.Add([]);
        }

        var severities = Effective(rulesets);

        // A folder whose Directory.Build.props selects its own ruleset (tests with ordering off) is a scope.
        var folderScopes = new List<Scope>();
        foreach (var (folder, ruleset) in folderRulesets.Where(f => f.Ruleset != rootRuleset))
        {
            var differences = Effective([ReadRuleset(ruleset, null)])
                .Where(p => !severities.TryGetValue(p.Key, out var s) || s != p.Value)
                .ToDictionary(p => p.Key, p => p.Value);
            if (differences.Count > 0)
            {
                folderScopes.Add(new Scope(Path.GetRelativePath(root, Path.Combine(folder, ".editorconfig")), "*.cs", differences));
            }
        }

        JsonElement? settings = null;
        var json = files.Where(f => Path.GetFileName(f).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
            .Concat(references.StyleCopJson)
            .FirstOrDefault();
        if (json is not null)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(json), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            if (document.RootElement.TryGetProperty("settings", out var element))
            {
                settings = element.Clone();
            }

            sources.Add(Path.GetRelativePath(root, json));
        }

        // Only where StyleCop ran (owner's decision 2026-10-10): the settings there, with the scopes that cover all of it.
        var allScopes = new List<Scope>([.. folderScopes, .. scopes]);
        var projects = Projects(root, files);
        var folders = Cover(projects.Uses, true, root);
        var without = folders is null ? [] : Cover(projects.Uses, false, root) ?? [];
        if (folders is not null)
        {
            allScopes = InStyleCopFolders(allScopes, folders, without, severities);
            foreach (var id in old is null ? [] : severities.Keys.Where(id => !old.Contains(id)).ToList())
            {
                severities[id] = Severity.None;
            }
        }

        var copier = msbuild.FirstOrDefault(m => CopiesEditorConfig.IsMatch(WithoutComments(m.Text))).File;
        string Relative(string file) => Path.GetRelativePath(root, file).Replace('\\', '/');
        return new StyleCopSetup(severities, settings, sources, allScopes, version, documentationParsed)
        {
            EditorConfigCopiedBy = copier is null ? null : Path.GetRelativePath(root, copier),
            Folders = folders,
            FoldersWithout = without,
            ReferencedIn = projects.ReferencedIn.Select(Relative).ToList(),
            RemovedIn = projects.RemovedIn.Select(Relative).ToList(),
        };
    }

    /// <summary>
    /// Signs of a StyleCop setup, for 'init', which then points to the migration instead: stylecop.json, StyleCop rule
    /// ids in a config or ruleset, a StyleCop.Analyzers reference; also in files the repository's MSBuild files import.
    /// </summary>
    public static List<string> Evidence(string root)
    {
        var files = EnumerateFiles(root).ToList();
        var references = FollowReferences(root, files);
        var evidence = new List<string>();
        foreach (var file in files.Concat(references.MSBuild).Concat(references.Rulesets).Concat(references.StyleCopJson).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(root, file);
            if (Path.GetFileName(file).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(relative);
            }
            else if (Kind(file) == "ruleset" && Regex.IsMatch(File.ReadAllText(file), @"\bId\s*=\s*""S[AX]\d{4}"""))
            {
                evidence.Add($"{relative} (StyleCop rules)");
            }
            else if (Kind(file) is "editorconfig" or "globalconfig"
                && File.ReadLines(file).Any(l => Regex.IsMatch(l, @"^\s*(dotnet_diagnostic\.S[AX]\d{4}\b|dotnet_analyzer_diagnostic\.category-StyleCop)", RegexOptions.IgnoreCase)))
            {
                evidence.Add($"{relative} (StyleCop rule severities)");
            }
            else if (IsMSBuild(file) && Regex.IsMatch(WithoutComments(File.ReadAllText(file)), @"\b(Include|Update)\s*=\s*""StyleCop\.Analyzers", RegexOptions.IgnoreCase))
            {
                evidence.Add($"{relative} (StyleCop.Analyzers package)");
            }
        }

        return evidence;
    }

    public static Severity? ParseSeverity(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "none" => Severity.None,
            "silent" or "hidden" => Severity.Silent,
            "suggestion" or "info" => Severity.Suggestion,
            "warning" => Severity.Warning,
            "error" => Severity.Error,
            _ => null,
        };
    }

    /// <summary>
    /// The .editorconfig section (in the root one) for the C# files under the folders (relative, '/'-separated). Sibling
    /// folders that share a dotted name start no folder of <paramref name="without"/> has become one wildcard
    /// ('src/LiteBus.Inbox*' for LiteBus.Inbox, LiteBus.Inbox.Abstractions, ...), so the section still covers exactly the
    /// projects of <paramref name="folders"/>. It may also cover folders without a project: their files aren't compiled.
    /// </summary>
    public static string SectionFor(IReadOnlyList<string> folders, IReadOnlyList<string>? without = null)
    {
        var others = (without ?? []).Select(f => f.Split('/')).ToList();
        var entries = new List<(string[] Parts, bool Wildcard)>();
        foreach (var group in folders.Select(f => f.Split('/')).GroupBy(parts => (Parent: string.Join("/", parts.SkipLast(1)), Start: WildcardStart(parts, others))))
        {
            entries.AddRange(group.Key.Start is { } start && group.Count() > 1 ? [([.. group.First().SkipLast(1), start], true)] : group.Select(p => (p, false)));
        }

        // The folders' common parent goes in front: 'src/{A,B}/**.cs'.
        var common = entries.Count == 1 ? 0 : Enumerable.Range(0, entries.Min(e => e.Parts.Length) - 1).TakeWhile(i => entries.All(e => e.Parts[i].Equals(entries[0].Parts[i], StringComparison.Ordinal))).Count();
        static string Escape(IEnumerable<string> segments) => Regex.Replace(string.Join("/", segments), @"[\[\]{}*?,\\]", @"\$0");
        var prefix = common == 0 ? string.Empty : Escape(entries[0].Parts.Take(common)) + "/";
        var rest = entries.Select(e => Escape(e.Parts.Skip(common)) + (e.Wildcard ? "*" : string.Empty)).ToList();
        return prefix + (rest.Count == 1 ? rest[0] : "{" + string.Join(",", rest) + "}") + "/**.cs";

        // The shortest dotted start of the folder's name (or the whole name) that no sibling folder of 'others' begins with.
        static string? WildcardStart(string[] parts, List<string[]> others)
        {
            var depth = parts.Length - 1;
            var name = parts[depth];
            var siblings = others.Where(o => o.Length > depth && o.Take(depth).SequenceEqual(parts.Take(depth), StringComparer.OrdinalIgnoreCase)).Select(o => o[depth]).ToList();
            return Enumerable.Range(1, name.Length).Where(i => i == name.Length || name[i] == '.').Select(i => name[..i])
                .FirstOrDefault(start => !siblings.Any(s => s.StartsWith(start, StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <summary>Whether an .editorconfig section applies to every C# file ('[*]', '[*.cs]', '[*.{cs,vb}]').</summary>
    public static bool AppliesToCSharp(string section)
    {
        if (section is "*" or "*.cs" or "**.cs" or "**/*.cs")
        {
            return true;
        }

        var braces = Regex.Match(section, @"^\*\.\{([^}]*)\}$");
        return braces.Success && braces.Groups[1].Value.Split(',').Any(e => e.Trim() == "cs");
    }

    /// <summary>
    /// Files in the repository, skipping build output, version control and nested repositories (git submodules and other
    /// folders with their own '.git' file or folder): their settings and code belong to another repository.
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                yield return file;
            }

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(child);
                if (name is not ("bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts") && !IsNestedRepository(child))
                {
                    pending.Push(child);
                }
            }
        }
    }

    /// <summary>The folders under the root that are their own git repositories (submodules), relative, with a trailing '/'.</summary>
    public static List<string> NestedRepositories(string root)
    {
        var result = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            foreach (var child in Directory.EnumerateDirectories(pending.Pop()))
            {
                if (Path.GetFileName(child) is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts")
                {
                    continue;
                }

                if (IsNestedRepository(child))
                {
                    result.Add(Path.GetRelativePath(root, child).Replace(Path.DirectorySeparatorChar, '/') + "/");
                }
                else
                {
                    pending.Push(child);
                }
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    /// <summary>Whether a folder is its own git repository (a submodule has a '.git' file, a nested clone a '.git' folder).</summary>
    public static bool IsNestedRepository(string directory)
    {
        var git = Path.Combine(directory, ".git");
        return File.Exists(git) || Directory.Exists(git);
    }

    /// <summary>A .csproj, .props or .targets file.</summary>
    public static bool IsMSBuild(string file)
    {
        return file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsOn(string id) => Severities.TryGetValue(id, out var severity) && severity >= Severity.Suggestion;

    /// <summary>The setup as seen inside a scope: the base severities with the scope's on top.</summary>
    public StyleCopSetup For(Scope scope)
    {
        var severities = Severities.ToDictionary(p => p.Key, p => p.Value);
        foreach (var (id, severity) in scope.Severities)
        {
            severities[id] = severity;
        }

        return new StyleCopSetup(severities, Settings, Sources, [], Version, DocumentationParsed);
    }

    /// <summary>A stylecop.json setting, like ("orderingRules", "usingDirectivesPlacement").</summary>
    public JsonElement? Setting(string section, string name)
    {
        return Settings is { } settings
            && settings.TryGetProperty(section, out var sectionElement)
            && sectionElement.ValueKind == JsonValueKind.Object
            && sectionElement.TryGetProperty(name, out var value)
                ? value
                : null;
    }

    /// <summary>
    /// Each project (full path) under the root and whether StyleCop runs in it: a StyleCop.Analyzers reference in the project,
    /// the Directory.Build.props/.targets or Directory.Packages.props it gets, or what those import, and no Remove item for it.
    /// Also the repository's own files that reference it and that remove it.
    /// </summary>
    internal static (Dictionary<string, bool> Uses, List<string> ReferencedIn, List<string> RemovedIn) Projects(string root, IReadOnlyCollection<string> files)
    {
        // ponytail: text-level, like FolderRulesets: Conditions are ignored, a Remove anywhere in the chain wins.
        var own = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        var rootFolder = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var uses = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var referenced = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var removed = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            var chain = new List<string> { project };
            foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
            {
                for (var folder = Path.GetDirectoryName(project); folder is not null && folder.Length >= rootFolder.Length; folder = Path.GetDirectoryName(folder))
                {
                    if (File.Exists(Path.Combine(folder, name)))
                    {
                        chain.Add(Path.Combine(folder, name));
                        break;
                    }
                }
            }

            for (var i = 0; i < chain.Count && i < 64; i++)
            {
                foreach (Match match in Reference.Matches(WithoutComments(File.ReadAllText(chain[i]))))
                {
                    if (match.Groups["import"].Success && Resolve(match.Groups["import"].Value, chain[i], root) is { } imported && IsMSBuild(imported)
                        && !chain.Contains(imported, StringComparer.OrdinalIgnoreCase))
                    {
                        chain.Add(imported);
                    }
                }
            }

            var references = chain.Where(f => StyleCopReference.IsMatch(WithoutComments(File.ReadAllText(f)))).ToList();
            var removes = chain.Where(f => StyleCopRemove.IsMatch(WithoutComments(File.ReadAllText(f)))).ToList();
            uses[project] = references.Count > 0 && removes.Count == 0;
            referenced.UnionWith(references.Where(own.Contains));
            removed.UnionWith(removes.Where(own.Contains));
        }

        return (uses, referenced.ToList(), removed.ToList());
    }

    /// <summary>
    /// The topmost folders (relative, '/'-separated) whose projects all have <paramref name="value"/>, from each such
    /// project's folder up while no project with the other value is below. Null when not some projects have each value
    /// (or, for StyleCop's folders, one is the root itself).
    /// </summary>
    internal static List<string>? Cover(IReadOnlyDictionary<string, bool> uses, bool value, string root)
    {
        if (!uses.Values.Contains(true) || !uses.Values.Contains(false))
        {
            return null;
        }

        var rootFolder = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var others = uses.Where(p => p.Value != value).Select(p => Path.GetDirectoryName(p.Key)!).ToList();
        bool HasOther(string folder) => others.Any(o => o.Equals(folder, StringComparison.OrdinalIgnoreCase) || o.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        var result = new List<string>();
        foreach (var project in uses.Where(p => p.Value == value).Select(p => p.Key))
        {
            // ponytail: a project sharing its folder with one of the other kind gets that folder anyway; per-file sections if that matters.
            var folder = Path.GetDirectoryName(project)!;
            while (Path.GetDirectoryName(folder) is { } parent && parent.Length > rootFolder.Length && !HasOther(parent))
            {
                folder = parent;
            }

            result.Add(Path.GetRelativePath(rootFolder, folder).Replace('\\', '/'));
        }

        result = Migration.Topmost(result);
        return value && result.Contains(".") ? null : result;
    }

    /// <summary>
    /// The files the repository's own MSBuild files point to, followed through imports into any folder under the root,
    /// submodules included (a shared-infrastructure submodule with the StyleCop setup): imported .props/.targets,
    /// CodeAnalysisRuleSet rulesets and stylecop.json additional files. They're only read, never written.
    /// </summary>
    internal static (List<string> MSBuild, List<string> Rulesets, List<string> StyleCopJson) FollowReferences(string root, IReadOnlyCollection<string> own)
    {
        var seen = new HashSet<string>(own, StringComparer.OrdinalIgnoreCase);
        var result = (MSBuild: new List<string>(), Rulesets: new List<string>(), StyleCopJson: new List<string>());
        var pending = new Queue<string>(own.Where(IsMSBuild));
        while (pending.Count > 0)
        {
            var file = pending.Dequeue();
            foreach (Match match in Reference.Matches(WithoutComments(File.ReadAllText(file))))
            {
                var group = new[] { "import", "ruleset", "additional" }.First(g => match.Groups[g].Success);
                if (Resolve(match.Groups[group].Value, file, root) is not { } target)
                {
                    continue;
                }

                if (group == "import" && IsMSBuild(target) && seen.Add(target))
                {
                    result.MSBuild.Add(target);
                    pending.Enqueue(target);
                }
                else if (group == "ruleset")
                {
                    result.Rulesets.Add(target);
                }
                else if (group == "additional" && Path.GetFileName(target).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
                {
                    result.StyleCopJson.Add(target);
                }
            }
        }

        return result;
    }

    internal static string WithoutComments(string xml) => Regex.Replace(xml, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

    /// <summary>The MSBuild files an MSBuild file imports that can be found (<see cref="Resolve"/>).</summary>
    internal static IEnumerable<string> Imports(string file, string root) =>
        Reference.Matches(WithoutComments(File.ReadAllText(file)))
            .Where(m => m.Groups["import"].Success)
            .Select(m => Resolve(m.Groups["import"].Value, file, root))
            .OfType<string>()
            .Where(IsMSBuild);

    /// <summary>Whether an .editorconfig section can apply to C# files at all ('[tests/**.cs]', '[*Tests.cs]').</summary>
    private static bool MayApplyToCSharp(string section)
    {
        return section.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(section, @"[{,]\s*cs\s*[,}]")
            || section.EndsWith("*", StringComparison.Ordinal);
    }

    /// <summary>
    /// The StyleCop severities each section of a .editorconfig or .globalconfig sets (a global config is one section):
    /// per rule, or per StyleCop category. Sections that can't apply to C# files are left out. <c>Bulk</c> holds the
    /// rules set only by a category or 'dotnet_analyzer_diagnostic.severity': in the compiler those don't override a
    /// rule's own entry in a ruleset or another config.
    /// </summary>
    private static List<(string Section, Dictionary<string, Severity> Severities, HashSet<string> Bulk)> ReadConfig(string file, bool global)
    {
        var sections = new List<(string Section, Dictionary<string, Severity> ByRule, Dictionary<string, Severity> ByCategory)>();
        (string Section, Dictionary<string, Severity> ByRule, Dictionary<string, Severity> ByCategory)? current =
            global ? (string.Empty, new(StringComparer.OrdinalIgnoreCase), new()) : null;
        foreach (var raw in File.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
            {
                continue;
            }

            if (line[0] == '[')
            {
                AddCurrent();
                var section = line.Trim('[', ']');
                current = global || MayApplyToCSharp(section) ? (section, new(StringComparer.OrdinalIgnoreCase), new()) : null;
                continue;
            }

            var equals = line.IndexOf('=');
            if (current is not { } target || equals < 0)
            {
                continue;
            }

            var key = line.Substring(0, equals).Trim();
            var value = line.Substring(equals + 1).Split('#', ';')[0];
            if (ParseSeverity(value) is not { } severity)
            {
                continue;
            }

            if (DiagnosticKey.Match(key) is { Success: true } rule)
            {
                target.ByRule[rule.Groups[1].Value] = severity;
            }
            else if (CategoryKey.Match(key) is { Success: true } category)
            {
                target.ByCategory[category.Groups[1].Value] = severity;
            }
            else if (key.Equals("dotnet_analyzer_diagnostic.severity", StringComparison.OrdinalIgnoreCase))
            {
                target.ByCategory[AllRules] = severity;
            }
        }

        AddCurrent();

        var result = new List<(string, Dictionary<string, Severity>, HashSet<string>)>();
        foreach (var (section, byRule, byCategory) in sections)
        {
            var severities = new Dictionary<string, Severity>();
            var bulk = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (id, category, defaultSeverity, _) in Rules)
            {
                // 'dotnet_analyzer_diagnostic.severity' (every rule; a folder of vendored code often sets it to none)
                // comes last, and like in the compiler it doesn't turn on rules that are off by default.
                if (byRule.TryGetValue(id, out var severity) || byCategory.TryGetValue(category, out severity)
                    || (byCategory.TryGetValue(AllRules, out severity) && (defaultSeverity > Severity.None || severity == Severity.None)))
                {
                    severities[id] = severity;
                    if (!byRule.ContainsKey(id))
                    {
                        bulk.Add(id);
                    }
                }
            }

            if (severities.Count > 0)
            {
                result.Add((section, severities, bulk));
            }
        }

        return result;

        void AddCurrent()
        {
            if (current is { } c)
            {
                sections.Add(c);
            }
        }
    }

    /// <summary>
    /// A ruleset's StyleCop rules, over the rulesets it includes ('Default' keeps their actions, another action replaces
    /// them, 'None' leaves them out). Included files are added to <paramref name="included"/>.
    /// </summary>
    private static Dictionary<string, Severity> ReadRuleset(string file, ISet<string>? included, int depth = 0)
    {
        var result = new Dictionary<string, Severity>();
        try
        {
            var document = XDocument.Load(file);
            foreach (var include in document.Descendants("Include"))
            {
                var path = (string?)include.Attribute("Path");
                var action = (string?)include.Attribute("Action") ?? "Default";
                var target = path is null ? null : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, path.Replace('\\', Path.DirectorySeparatorChar)));
                if (target is null || depth > 8 || action.Equals("None", StringComparison.OrdinalIgnoreCase) || !File.Exists(target))
                {
                    continue;
                }

                included?.Add(target);
                var forced = ParseSeverity(action);
                foreach (var (id, severity) in ReadRuleset(target, included, depth + 1))
                {
                    result[id] = forced is { } f && severity > Severity.None ? f : severity;
                }
            }

            foreach (var rule in document.Descendants("Rule"))
            {
                var id = (string?)rule.Attribute("Id");
                var action = (string?)rule.Attribute("Action");
                if (id is not null && (id.StartsWith("SA", StringComparison.Ordinal) || id.StartsWith("SX", StringComparison.Ordinal)) && action is not null && ParseSeverity(action) is { } severity)
                {
                    result[id] = severity;
                }
            }
        }
        catch (System.Xml.XmlException)
        {
            // Not a ruleset after all.
        }

        return result;
    }

    /// <summary>
    /// The ruleset each folder with its own Directory.Build.props (the root's too) gives its projects: the last
    /// CodeAnalysisRuleSet the props and its imports set, in order ("'$(CodeAnalysisRuleSet)' == ''" conditions respected).
    /// A value relative to the project is resolved from the folder's projects.
    /// </summary>
    private static List<(string Folder, string Ruleset)> FolderRulesets(IReadOnlyList<string> own, string root)
    {
        // ponytail: text-level evaluation (no PropertyGroup conditions, no other properties); an unknown value means no scope.
        var result = new List<(string, string)>();
        foreach (var props in own.Where(f => Path.GetFileName(f).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)))
        {
            var folder = Path.GetDirectoryName(props)!;
            var projects = own.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && f.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).ToList();
            string? ruleset = null;
            Evaluate(props, 0);
            if (ruleset is not null)
            {
                result.Add((folder, ruleset));
            }

            void Evaluate(string file, int depth)
            {
                foreach (Match match in Reference.Matches(WithoutComments(File.ReadAllText(file))))
                {
                    if (match.Groups["import"].Success)
                    {
                        if (depth < 16 && Resolve(match.Groups["import"].Value, file, root) is { } imported && IsMSBuild(imported))
                        {
                            Evaluate(imported, depth + 1);
                        }
                    }
                    else if (match.Groups["ruleset"].Success && (ruleset is null || !match.Groups["condition"].Value.Contains("$(CodeAnalysisRuleSet)", StringComparison.OrdinalIgnoreCase)))
                    {
                        var value = match.Groups["ruleset"].Value;
                        ruleset = Resolve(value, file, root) ?? projects.Select(p => Resolve(value, p, root)).FirstOrDefault(r => r is not null);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// An MSBuild path as an existing file under the root, or null: $(MSBuildThisFileDirectory) and GetPathOfFileAbove are
    /// filled in, a relative path is taken from the file's folder, anything with other properties or wildcards is unknown.
    /// </summary>
    private static string? Resolve(string value, string file, string root)
    {
        var directory = Path.GetDirectoryName(file)! + Path.DirectorySeparatorChar;
        value = value.Trim().Replace("$(MSBuildThisFileDirectory)", directory, StringComparison.OrdinalIgnoreCase);
        var above = Regex.Match(value, @"^\$\(\[MSBuild\]::GetPathOfFileAbove\(\s*'([^']+)'\s*(?:,\s*'([^']*)')?\s*\)\)$", RegexOptions.IgnoreCase);
        if (above.Success)
        {
            var start = above.Groups[2].Success ? Path.GetFullPath(Path.Combine(directory, above.Groups[2].Value)) : directory;
            value = string.Empty;
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, above.Groups[1].Value)))
                {
                    value = Path.Combine(folder.FullName, above.Groups[1].Value);
                    break;
                }
            }
        }

        if (value.Length == 0 || value.Contains("$(", StringComparison.Ordinal) || value.Contains("@(", StringComparison.Ordinal) || value.Contains('*') || value.Contains(';'))
        {
            return null;
        }

        var full = Path.GetFullPath(Path.Combine(directory, value.Replace('\\', Path.DirectorySeparatorChar)));
        var rootFolder = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return File.Exists(full) && full.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    /// <summary>
    /// The scopes, where StyleCop runs only in <paramref name="folders"/>: a scope inside one of them stays; one for all C#
    /// files that covers every one of them is folded into <paramref name="severities"/> (their base); one that covers some is
    /// moved to a root section for those; one that covers none is dropped (no StyleCop there, so no settings either). A
    /// file-pattern section above several folders stays as it is.
    /// </summary>
    private static List<Scope> InStyleCopFolders(IEnumerable<Scope> scopes, IReadOnlyList<string> folders, IReadOnlyList<string> without, Dictionary<string, Severity> severities)
    {
        static bool IsUnder(string folder, string parent) =>
            parent.Length == 0 || folder.Equals(parent, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase);

        var result = new List<Scope>();
        var located = scopes.Select(scope =>
        {
            // A path section ('tests/**.cs') narrows the folder by its literal leading segments.
            var folder = (Path.GetDirectoryName(scope.File) ?? string.Empty).Replace('\\', '/');
            var segments = scope.Section.TrimStart('/').Split('/');
            var literal = segments.Length > 1 ? segments.Take(segments.Length - 1).TakeWhile(p => p.IndexOfAny(['*', '?', '[', '{']) < 0).ToList() : [];
            var area = string.Join("/", new[] { folder }.Concat(literal).Where(p => p.Length > 0));
            var rest = string.Join("/", segments.Skip(literal.Count));
            return (Scope: scope, Area: area, AllCSharp: AppliesToCSharp(rest));
        });
        foreach (var (scope, area, allCSharp) in located.OrderBy(l => l.Area.Count(c => c == '/') + (l.Area.Length > 0 ? 1 : 0)))
        {
            var covered = folders.Where(f => IsUnder(f, area)).ToList();
            if (folders.Any(f => IsUnder(area, f)) || (covered.Count > 0 && !allCSharp))
            {
                result.Add(scope);
            }
            else if (covered.Count == folders.Count)
            {
                Overlay(severities, scope.Severities);
            }
            else if (covered.Count > 0)
            {
                result.Add(new Scope(".editorconfig", SectionFor(covered, [.. without, .. folders.Except(covered)]), scope.Severities));
            }
        }

        return result;
    }

    /// <summary>Each rule the configs set, at the strictest of them; a config that doesn't set it counts with the target's value.</summary>
    private static void OverlayStrictest(Dictionary<string, Severity> target, IReadOnlyList<Dictionary<string, Severity>> configs)
    {
        foreach (var id in configs.SelectMany(c => c.Keys).Distinct().ToList())
        {
            var before = target.TryGetValue(id, out var b) ? b : Severity.None;
            target[id] = configs.Max(c => c.TryGetValue(id, out var s) ? s : before);
        }
    }

    private static void Overlay(Dictionary<string, Severity> target, IReadOnlyDictionary<string, Severity> source)
    {
        foreach (var (id, severity) in source)
        {
            target[id] = severity;
        }
    }

    private static string? Kind(string file)
    {
        var name = Path.GetFileName(file);
        return name.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase) ? "editorconfig"
            : name.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase) ? "globalconfig"
            : name.EndsWith(".ruleset", StringComparison.OrdinalIgnoreCase) ? "ruleset"
            : null;
    }

    private static HashSet<string> LoadIds(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} isn't embedded.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            ids.Add(line.Split(',')[0].Trim('"'));
        }

        return ids;
    }

    private static IReadOnlyList<(string Id, string Category, Severity Default, string Title)> LoadRules()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StyleCopRules.csv")
            ?? throw new InvalidOperationException("The StyleCop rule inventory isn't embedded.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rules = new List<(string, string, Severity, string)>();
        while (reader.ReadLine() is { } line)
        {
            // Id,Title,Category,EnabledByDefault,DefaultSeverity,StyleCopHasFix,HelpLink (Title may contain commas, quoted)
            var fields = Regex.Matches(line, "\"([^\"]*)\"|([^,]+)|(?<=,)(?=,)").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Value).ToList();
            if (fields.Count < 5)
            {
                continue;
            }

            var enabled = fields[3].Equals("True", StringComparison.OrdinalIgnoreCase);
            var severity = enabled ? ParseSeverity(fields[4]) ?? Severity.Warning : Severity.None;
            rules.Add((fields[0], fields[2], severity, fields[1]));
        }

        return rules;
    }
}
