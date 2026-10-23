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
/// path-specific section of the root one (like '[tests/**.cs]'). <see cref="Severities"/> holds only what it sets;
/// <see cref="Settings"/> is the stylecop.json its projects use when it isn't the repository-wide one; <see cref="Sonar"/>
/// the Sonar severities it sets (<see cref="SonarSetup.For"/>), or null.
/// </summary>
internal sealed record Scope(string File, string Section, IReadOnlyDictionary<string, Severity> Severities, JsonElement? Settings = null, IReadOnlyDictionary<string, Severity>? Sonar = null);

/// <summary>
/// StyleSharp.Analyzers (SST ids, a StyleCop successor) in a repository, read like StyleCop (owner's decision 2026-10-14):
/// its version (null when unknown), its options ('stylesharp.*' keys and file_header_template, from the root .editorconfig's
/// sections for all C# and global configs; StyleSharp doesn't read stylecop.json), and whether StyleCop runs next to it (a
/// rule then counts as on when either enforces it).
/// </summary>
internal sealed record StyleSharpUse(string? Version, IReadOnlyDictionary<string, string> Options, bool WithStyleCop = false)
{
    /// <summary>
    /// The words SST1305 accepts as a name's start that StyleCop's SA1305 list (BRO1310's 'common' prefixes) doesn't have:
    /// two-letter words and technology names ('idText', 'okResponse', 'dbContext'; StyleSharp 5.1.2's list).
    /// </summary>
    public static readonly string[] HungarianWords = ["db", "ef", "gc", "id", "io", "js", "ms", "my", "ok", "so", "ui", "up", "us"];

    /// <summary>An option's value: the rule's own key ('stylesharp.SST1316.tuple_element_naming') over the shared one, or null.</summary>
    public string? Option(string name, string? rule = null) =>
        (rule is not null && Options.TryGetValue($"stylesharp.{rule}.{name}", out var own) ? own : null)
        ?? (Options.TryGetValue($"stylesharp.{name}", out var shared) ? shared : null);

    /// <summary>
    /// The options as stylecop.json's 'settings' (what <see cref="Migration"/> reads): no XML header (StyleSharp's header is
    /// file_header_template's plain one), usings outside the namespace (SST1200), which members need documentation, the
    /// tuple element casing, the Hungarian prefixes allowed (StyleSharp's words and its setting's) and the documentation
    /// elements whose text needs no period.
    /// </summary>
    public JsonElement Settings()
    {
        // SST1629 checks the period in summary, remarks, returns and value only, and not after an element at the end (trial 24:
        // BRO1603 put one after Refit's '<exception cref="ArgumentNullException">value</exception>' and '... <see cref="T"/>').
        var documentation = new System.Text.Json.Nodes.JsonObject
        {
            ["xmlHeader"] = false,
            ["excludeFromPunctuationCheck"] = new System.Text.Json.Nodes.JsonArray("seealso", "param", "typeparam", "exception", "see", "paramref", "typeparamref", "c", "a", "b", "i", "u", "em", "strong", "sub", "sup", "span", "tt"),
        };
        if (Options.TryGetValue("file_header_template", out var template) && template.Length > 0)
        {
            documentation["copyrightText"] = template.Replace("\\n", "\n", StringComparison.Ordinal);
        }

        foreach (var (key, name) in new[] { ("document_exposed_elements", "documentExposedElements"), ("document_internal_elements", "documentInternalElements"), ("document_private_elements", "documentPrivateElements") })
        {
            if (bool.TryParse(Option(key), out var value))
            {
                documentation[name] = value;
            }
        }

        var naming = new System.Text.Json.Nodes.JsonObject();
        if (Option("tuple_element_naming", "SST1316") is { } casing)
        {
            naming["tupleElementNameCasing"] = casing.Replace("_", string.Empty, StringComparison.Ordinal).Equals("camelcase", StringComparison.OrdinalIgnoreCase) ? "camelCase" : "PascalCase";
        }

        // SST1305 allows more words than SA1305 (HungarianWords), plus its own setting (a list split by commas, semicolons or
        // spaces, either case: lower-cased here, BRO1310 compares exactly).
        var prefixes = (Option("allowed_hungarian_prefixes", "SST1305") ?? string.Empty).Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(p => p.ToLowerInvariant());
        naming["allowedHungarianPrefixes"] = new System.Text.Json.Nodes.JsonArray(HungarianWords.Concat(prefixes).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(p => (System.Text.Json.Nodes.JsonNode?)p).ToArray());

        var settings = new System.Text.Json.Nodes.JsonObject
        {
            ["documentationRules"] = documentation,
            ["namingRules"] = naming,
            ["orderingRules"] = new System.Text.Json.Nodes.JsonObject { ["usingDirectivesPlacement"] = "outsideNamespace" },
        };
        using var document = JsonDocument.Parse(settings.ToJsonString());
        return document.RootElement.Clone();
    }
}

/// <summary>The ruleset and stylecop.json (full paths, or null) a project's StyleCop uses.</summary>
internal sealed record ProjectStyleCop(string? Ruleset, string? Json);

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
    /// <summary>The start of the warning for StyleCop projects without restore output (<see cref="PackageGlobalConfigs"/>).</summary>
    internal const string NotRestored = "Not restored:";

    /// <summary>How the report prints that warning (<see cref="NotRestored"/>), which the preview passes on.</summary>
    internal const string NotRestoredWarning = "Warning: " + NotRestored;

    /// <summary>The key ReadConfig files a 'dotnet_analyzer_diagnostic.severity' (every rule) under, next to the categories.</summary>
    private const string AllRules = "*";

    /// <summary>The per-framework evaluations <see cref="EvaluateStyleCop"/> ran (project|framework), once per process.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<string>> FrameworkEvaluations = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex DiagnosticKey = new(@"^dotnet_diagnostic\.(S[AX]\d{4}\w*|SST\d{4})\.severity$", RegexOptions.IgnoreCase);
    private static readonly Regex SonarKey = new(@"^dotnet_diagnostic\.(S\d+)\.severity$", RegexOptions.IgnoreCase);

    /// <summary>
    /// StyleCop's version: a PackageReference/PackageVersion (attribute or element), a packages.config entry, or an analyzer's
    /// hint path into a packages.config 'packages' folder ('..\packages\StyleCop.Analyzers.1.0.2\analyzers\...': xbox-live-developer-tools).
    /// </summary>
    private static readonly Regex PackageVersion = VersionPattern(@"StyleCop\.Analyzers(?:\.Unstable)?");

    /// <summary>StyleSharp's version, found the same ways as StyleCop's (<see cref="PackageVersion"/>).</summary>
    private static readonly Regex StyleSharpVersion = VersionPattern(@"StyleSharp\.Analyzers");
    private static readonly Regex DocumentationFile = new(@"<GenerateDocumentationFile>\s*true\s*<|<DocumentationFile>", RegexOptions.IgnoreCase);

    /// <summary>A category's bulk severity: StyleCop's ('StyleCop.CSharp.NamingRules') or StyleSharp's (plain: 'Naming').</summary>
    private static readonly Regex CategoryKey = new(@"^dotnet_analyzer_diagnostic\.category-(?:StyleCop\.CSharp\.)?(\w+)\.severity$", RegexOptions.IgnoreCase);

    /// <summary>What an MSBuild file points to: an import, a ruleset, an additional file (stylecop.json).</summary>
    private static readonly Regex Reference = new(
        @"<Import\b[^>]*?\bProject\s*=\s*""(?<import>[^""]+)""|<CodeAnalysisRuleSet\b(?:[^>]*?\bCondition\s*=\s*""(?<condition>[^""]*)"")?[^>]*>(?<ruleset>[^<]+)</CodeAnalysisRuleSet>|<AdditionalFiles\b[^>]*?\bInclude\s*=\s*""(?<additional>[^""]+)""",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// A reference that runs StyleCop in a project: a (global) package reference or an analyzer DLL, of StyleCop.Analyzers or
    /// StyleSharp.Analyzers (a StyleCop successor, read the same way: <see cref="StyleSharpUse"/>).
    /// </summary>
    /// <summary>A package reference that isn't StyleCop's, StyleSharp's or StyleBro's: one that could bring a global config.</summary>
    private static readonly Regex OtherPackage = new(@"<(?:Global)?PackageReference\b[^>]*?\bInclude\s*=\s*""(?!(?:StyleCop|StyleSharp|StyleBro)\.Analyzers)[^""]+""", RegexOptions.IgnoreCase);

    private static readonly Regex StyleCopReference = new(@"<(?:Global)?PackageReference\b[^>]*?\bInclude\s*=\s*""(?:StyleCop\.Analyzers(?:\.Unstable)?|StyleSharp\.Analyzers)""|<Analyzer\b[^>]*?\bInclude\s*=\s*""[^""]*(?:StyleCop|StyleSharp)\.Analyzers", RegexOptions.IgnoreCase);

    /// <summary>A project that opts out of the StyleCop reference it inherits ('&lt;PackageReference Remove="StyleCop.Analyzers" /&gt;').</summary>
    private static readonly Regex StyleCopRemove = new(@"<PackageReference\b[^>]*?\bRemove\s*=\s*""(?:StyleCop\.Analyzers(?:\.Unstable)?|StyleSharp\.Analyzers)""", RegexOptions.IgnoreCase);

    /// <summary>A StyleSharp.Analyzers reference or central version in an MSBuild file.</summary>
    private static readonly Regex StyleSharpReference = new(@"\b(?:Include|Update)\s*=\s*""StyleSharp\.Analyzers""|<Analyzer\b[^>]*?\bInclude\s*=\s*""[^""]*StyleSharp\.Analyzers", RegexOptions.IgnoreCase);

    /// <summary>A StyleCop.Analyzers reference or central version in an MSBuild file.</summary>
    private static readonly Regex StyleCopPackage = new(@"\b(?:Include|Update)\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""|<Analyzer\b[^>]*?\bInclude\s*=\s*""[^""]*StyleCop\.Analyzers", RegexOptions.IgnoreCase);

    /// <summary>
    /// An item or Copy task taking an .editorconfig (not one the compiler reads as a config file, nor the SDK's item types,
    /// which at most copy it to the output folder: trial 28, EpubReader's '&lt;None Include="..\.editorconfig"&gt;' with a Link
    /// only shows it in Solution Explorer).
    /// </summary>
    private static readonly Regex CopiesEditorConfig = new(@"<(?!(?:EditorConfigFiles|GlobalAnalyzerConfigFiles|None|Content|Compile|AdditionalFiles|EmbeddedResource|Folder|Page|Resource)\s)[\w.]+\s[^>]*\b(?:Include|SourceFiles)\s*=\s*""[^""]*\.editorconfig""", RegexOptions.IgnoreCase);

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

    /// <summary>
    /// Gets styleSharp's rules (SST ids): id, category, default severity and title, and the StyleCop rules each stands for
    /// (scripts/stylecop-survey/New-StyleSharpMapping.ps1; none for a rule StyleCop doesn't have).
    /// </summary>
    public static IReadOnlyList<(string Id, string Category, Severity Default, string Title, IReadOnlyList<string> StyleCop)> StyleSharpRules { get; } = LoadStyleSharpRules();

    /// <summary>Gets every StyleCop rule's effective severity (with StyleSharp, read from its SST rules: <see cref="Translate"/>).</summary>
    public IReadOnlyDictionary<string, Severity> Severities { get; }

    /// <summary>Gets styleSharp.Analyzers' use in the repository, or null when it doesn't run StyleSharp.</summary>
    public StyleSharpUse? StyleSharp { get; init; }

    /// <summary>Gets the severities as the configs set them (SA, SX and SST ids), before <see cref="Translate"/>; null without StyleSharp.</summary>
    public IReadOnlyDictionary<string, Severity>? Raw { get; init; }

    /// <summary>Gets the analyzer package(s) the setup is read from, for the report: 'StyleCop.Analyzers', 'StyleSharp.Analyzers' or both.</summary>
    public string PackageName => StyleSharp is null ? "StyleCop.Analyzers" : StyleSharp.WithStyleCop ? "StyleCop.Analyzers and StyleSharp.Analyzers" : "StyleSharp.Analyzers";

    /// <summary>Gets the 'settings' object of stylecop.json, or null when there's none.</summary>
    public JsonElement? Settings { get; }

    /// <summary>Gets the files the setup was read from, for the report.</summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>Gets the StyleCop.Analyzers version the repository references, or null when none was found.</summary>
    public string? Version { get; }

    /// <summary>Gets a value indicating whether <see cref="Version"/> comes from the block an earlier --write wrote (no reference left).</summary>
    public bool VersionRecorded { get; init; }

    /// <summary>Gets a value indicating whether <see cref="Settings"/> are the ones the last --write recorded (stylecop.json is gone).</summary>
    public bool SettingsRecorded { get; init; }

    /// <summary>Gets the version text of the StyleCop.Analyzers reference when it names a property no MSBuild file defines ('$(X)'), else null.</summary>
    public string? UnresolvedVersion { get; init; }

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

    /// <summary>Gets what the report must point out: settings this reader couldn't map exactly.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Gets the MSBuild files (relative) that reference StyleCop.Analyzers.</summary>
    public IReadOnlyList<string> ReferencedIn { get; init; } = [];

    /// <summary>Gets the MSBuild files (relative) outside the repository's own (a git submodule's) that reference StyleCop.Analyzers.</summary>
    public IReadOnlyList<string> ReferencedOutside { get; init; } = [];

    /// <summary>Gets the MSBuild files (relative) that remove the StyleCop.Analyzers reference they inherit.</summary>
    public IReadOnlyList<string> RemovedIn { get; init; } = [];

    /// <summary>Gets a value indicating whether no project runs StyleCop (<see cref="IsInstalled"/>): its settings are leftovers.</summary>
    public bool NotInstalled { get; init; }

    /// <summary>Gets the rules the configs are read for: StyleCop's and StyleSharp's (declared after both, which it reads).</summary>
    private static IReadOnlyList<(string Id, string Category, Severity Default)> ReadRules { get; } =
        [.. Rules.Select(r => (r.Id, r.Category, r.Default)), .. StyleSharpRules.Select(r => (r.Id, r.Category, r.Default))];

    /// <summary>
    /// Reads the setup. <paramref name="evaluate"/> tells whether StyleCop.Analyzers is among a project's evaluated items
    /// (null: MSBuild failed); by default 'dotnet msbuild' (<see cref="EvaluateStyleCop"/>).
    /// </summary>
    public static StyleCopSetup Read(string root, Func<string, bool?>? evaluate = null, Func<string, IReadOnlyList<string>?>? packageConfigs = null)
    {
        var files = EnumerateFiles(root).ToList();
        var references = FollowReferences(root, files);
        var sources = new List<string>();
        var scopes = new List<Scope>();

        // Rules added in StyleCop 1.2 (SA1651, SA1141, ...) don't exist in 1.1.x: off, whatever the configs say.
        var msbuild = files.Where(IsMSBuild).Concat(references.MSBuild).Select(f => (File: f, Text: Migration.ReadOrNull(f) ?? string.Empty)).ToList();
        var documentationParsed = msbuild.Any(m => DocumentationFile.IsMatch(m.Text));

        // A version from a property ('$(StyleCopVersion)', Steeltoe's versions.props) is resolved from the MSBuild files.
        var properties = msbuild.Select(m => WithoutComments(m.Text)).ToList();
        var packagesConfigs = files.Where(f => Path.GetFileName(f).Equals("packages.config", StringComparison.OrdinalIgnoreCase)).Select(f => Migration.ReadOrNull(f) ?? string.Empty).ToList();
        List<(string Raw, string? Value)> Versions(Regex pattern) => msbuild.Select(m => m.Text)
            .Concat(packagesConfigs)
            .SelectMany(text => pattern.Matches(text))
            .Select(m => m.Groups["v"].Value.Trim())
            .Select(v => (Raw: v, Value: ResolveProperties(v, properties)))
            .ToList();
        var versions = Versions(PackageVersion);
        var version = versions.Select(v => v.Value).OfType<string>().OrderBy(v => v, StringComparer.Ordinal).FirstOrDefault();
        var unresolved = version is null ? versions.Select(v => v.Raw).FirstOrDefault() : null;

        // After the package swap the reference is gone: the version the last --write recorded counts (kubernetes-client: a
        // second run turned on BRO1123, BRO1124 and BRO1311, which replace rules StyleCop 1.1.118 doesn't have).
        var recordedVersion = Migration.RecordedVersion(root);
        var recorded = version is null ? recordedVersion : null;
        version ??= recorded;
        var old = OldRules(version);

        // StyleSharp, StyleCop's successor with SST ids (Splat: SST1309 = error, '_camelCase', read as StyleCop's defaults
        // from a leftover stylecop.json, and 72 files lost their '_'): referenced, or recorded by the last --write.
        var recordedStyleSharp = Migration.RecordedVersion(root, Migration.StyleSharpVersionPrefix);
        var styleSharpVersions = Versions(StyleSharpVersion);
        var styleSharpReferenced = recordedStyleSharp is not null || styleSharpVersions.Count > 0 || msbuild.Any(m => StyleSharpReference.IsMatch(WithoutComments(m.Text)));
        var styleCopReferenced = version is not null || versions.Count > 0 || msbuild.Any(m => StyleCopPackage.IsMatch(WithoutComments(m.Text)));
        var styleSharpOnly = styleSharpReferenced && !styleCopReferenced;

        // Each project that runs StyleCop, with the ruleset and stylecop.json it gets (its own file, its Directory.Build.props
        // and .targets, and what they import). Only those projects count: a sample project without StyleCop has no ruleset
        // and would otherwise bring StyleCop's defaults back in (Soulseek.NET's examples turned SA1101 on: 191 files got
        // 'this.'). Without any StyleCop reference found, every project counts.
        var projects = Projects(root, files);
        var warnings = new List<string>();

        // Also when a file outside the repository's own still references it and the swap removed it (Fonts' submodule): the
        // block the last --write recorded is the migrated state, not leftovers.
        var installed = recordedVersion is not null || recordedStyleSharp is not null || Evaluate(root, projects.Uses, evaluate ?? EvaluateStyleCop, warnings);

        // After the package swap no project references StyleCop, and every project would count: the projects the last
        // --write found without it (the block's "No StyleCop ran in" line) still don't, so --write gives the same block
        // again (Autofac: SA1309 came on from its AOT test projects, and BRO1303 switched to 'camelCase').
        if ((recordedVersion ?? recordedStyleSharp) is not null && !projects.Uses.Values.Contains(true) && Migration.UnformattedFolders(root) is { Count: > 0 } recordedWithout)
        {
            var uses = projects.Uses.ToDictionary(p => p.Key, p => !recordedWithout.Any(folder => InFolder(root, p.Key, folder)), StringComparer.OrdinalIgnoreCase);
            projects = (uses, projects.ReferencedIn, projects.RemovedIn, projects.ReferencedOutside);
        }

        var allConfigs = projects.Uses.Keys.Order(StringComparer.OrdinalIgnoreCase).Select(p => (Project: p, Config: ProjectConfig(p, root))).ToList();
        var configs = allConfigs.Where(c => projects.Uses[c.Project] || !projects.Uses.Values.Contains(true)).ToList();

        // Rulesets: the repository's own and the ones its MSBuild files select (also in a submodule), each with the
        // rulesets it includes. A ruleset that is only included is read as part of the including one, whose overrides count.
        // A ruleset that only projects without StyleCop select doesn't count; one no project can be traced to (selected
        // through a property this reader can't follow) does.
        var selected = new HashSet<string>(references.Rulesets.Concat(allConfigs.Select(c => c.Config.Ruleset).OfType<string>()), StringComparer.OrdinalIgnoreCase);
        var styleCopRulesets = new HashSet<string>(configs.Select(c => c.Config.Ruleset).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        var otherRulesets = new HashSet<string>(allConfigs.Select(c => c.Config.Ruleset).OfType<string>().Where(r => !styleCopRulesets.Contains(r)), StringComparer.OrdinalIgnoreCase);
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var read = files.Where(f => Kind(f) == "ruleset").Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(f => (File: f, Rules: ReadRuleset(f, included)))
            .ToList();
        var rulesets = new List<Dictionary<string, Severity>>();
        foreach (var (file, rules) in read.Where(r => r.Rules.Count > 0 && (selected.Contains(r.File) || !included.Contains(r.File)) && !otherRulesets.Contains(r.File)))
        {
            rulesets.Add(rules);
            sources.Add(Path.GetRelativePath(root, file));
        }

        // Global configs a package adds (GlobalAnalyzerConfigFiles from its build props) count like the repository's own:
        // Pidgin's StyleCop settings are Benjamin.Pizza.BuildScripts' globalconfig, which turns SA1200, SA1101 and the
        // ordering rules off, and read as StyleCop's defaults they moved 100 files' usings and added 'this.' to 71 (trial 48).
        var packageFiles = PackageGlobalConfigs(root, configs.Select(c => c.Project), packageConfigs ?? (project => ReadPackageGlobalConfigs(project, root)), warnings);
        var globalConfigs = new List<(Dictionary<string, Severity> Severities, HashSet<string> Bulk)>();
        var packageConfigsRead = new List<(Dictionary<string, Severity> Severities, HashSet<string> Bulk)>();
        foreach (var file in files.Where(f => Kind(f) == "globalconfig").Concat(packageFiles))
        {
            var found = ReadConfig(file, global: true).Where(s => s.Severities.Count > 0).ToList();
            if (found.Count > 0)
            {
                var package = packageFiles.Contains(file);
                (package ? packageConfigsRead : globalConfigs).Add((found[0].Severities, found[0].Bulk));
                sources.Add(package ? $"{file} (a package's)" : Path.GetRelativePath(root, file));
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

            // Sonar's severities outside the root's sections for all C# make a scope too (SonarSetup reads the rest).
            var relative = Path.GetRelativePath(root, file);
            if (sections.Any(s => s.Severities.Count > 0))
            {
                sources.Add(relative);
            }

            foreach (var section in sections)
            {
                if (isRoot && AppliesToCSharp(section.Section))
                {
                    rootSections.Add((section.Severities, section.Bulk));
                }
                else
                {
                    scopes.Add(new Scope(relative, section.Section, section.Severities, Sonar: section.Sonar.Count > 0 ? section.Sonar : null));
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
                .Concat(packageConfigsRead.Concat(globalConfigs).Concat(rootSections).SelectMany(c => c.Severities.Keys.Where(id => !c.Bulk.Contains(id)))),
            StringComparer.Ordinal);
        Dictionary<string, Severity> WithoutBulkOverrides((Dictionary<string, Severity> Severities, HashSet<string> Bulk) config) =>
            config.Severities.Where(p => !config.Bulk.Contains(p.Key) || !specific.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        var packageInEffect = packageConfigsRead.Select(WithoutBulkOverrides).ToList();
        var globalInEffect = globalConfigs.Select(WithoutBulkOverrides).ToList();
        var rootInEffect = rootSections.Select(WithoutBulkOverrides).ToList();
        Dictionary<string, Severity> Effective(IReadOnlyList<Dictionary<string, Severity>> inEffect)
        {
            var result = ReadRules.ToDictionary(r => r.Id, r => r.Default);
            OverlayStrictest(result, inEffect);

            // A package's global config applies to the same projects as the repository's own (not one per project type):
            // a layer under them, so a repository file that doesn't set a rule keeps the package's value.
            packageInEffect.ForEach(config => Overlay(result, config));
            OverlayStrictest(result, globalInEffect);
            rootInEffect.ForEach(section => Overlay(result, section));
            foreach (var id in old is null ? [] : result.Keys.Where(id => !old.Contains(id) && !IsStyleSharpId(id)).ToList())
            {
                result[id] = Severity.None;
            }

            return result;
        }

        // A StyleCop project without a ruleset gets StyleCop's defaults: they count among the rulesets, else a test
        // project's ruleset would turn its rules off everywhere.
        if (rulesets.Count > 0 && configs.Any(c => c.Config.Ruleset is null))
        {
            rulesets.Add([]);
        }

        var severities = Effective(rulesets);

        // stylecop.json: the one most StyleCop projects link is the repository-wide one (without any, the shallowest file).
        // StyleSharp doesn't read it: in a StyleSharp repository it's a leftover (Splat's said camelCase fields), never read.
        var linked = configs.Select(c => c.Config.Json).OfType<string>().ToList();
        var json = linked.GroupBy(f => f, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.Key).FirstOrDefault()
            ?? files.Where(f => Path.GetFileName(f).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Concat(references.StyleCopJson)
                .FirstOrDefault();
        if (styleSharpOnly)
        {
            if (json is not null)
            {
                warnings.Add($"{Path.GetRelativePath(root, json).Replace('\\', '/')}: StyleSharp doesn't read stylecop.json, so its settings aren't read here either (a leftover from StyleCop); StyleSharp's own are its 'stylesharp.*' keys.");
            }

            (json, linked) = (null, []);
            configs = configs.Select(c => c with { Config = c.Config with { Json = null } }).ToList();
        }

        // After the swap, stylecop.json may be gone too (the report says it can go): the settings the last --write recorded count
        // (trial 25: a second --write turned stylebro_document_internal_elements back on and moved the usings inside).
        var settings = json is null ? null : ReadSettings(json);
        var settingsRecorded = json is null && !styleSharpOnly && recordedVersion is not null && Migration.RecordedSettings(root) is not null;
        if (settingsRecorded)
        {
            settings = Migration.RecordedSettings(root);
            sources.Add("stylecop.json's settings recorded by the last --write");
        }

        foreach (var file in new[] { json }.Concat(linked).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            sources.Add(Path.GetRelativePath(root, file));
        }

        // Projects whose ruleset or stylecop.json differs from the repository-wide result get a section of their own
        // (Soulseek.NET's src and tests each link their own, with different rules and license headers). Projects that share
        // a folder but not their settings can't be told apart by an .editorconfig section: warned about, never picked silently.
        var projectScopes = new List<Scope>();
        string FolderOf(string project) => Path.GetRelativePath(root, Path.GetDirectoryName(project)!).Replace('\\', '/');
        string JsonText(string? file) => file is null ? string.Empty : File.ReadAllText(file);
        var byFolder = configs.GroupBy(c => FolderOf(c.Project), StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var folder in byFolder.Where(g => g.Select(c => (c.Config.Ruleset?.ToUpperInvariant(), JsonText(c.Config.Json))).Distinct().Skip(1).Any()))
        {
            var names = folder.Select(c => Path.GetFileName(c.Project)).ToList();
            warnings.Add($"The projects in {folder.Key} ({string.Join(", ", names)}) use different StyleCop rulesets or stylecop.json files, and one folder gets one set of settings: {names[0]}'s are used there. Check the rules and file headers of the others.");
        }

        var firstPerFolder = byFolder.Select(g => (Folder: g.Key, g.First().Config)).ToList();
        foreach (var group in firstPerFolder.GroupBy(f => (Ruleset: f.Config.Ruleset?.ToUpperInvariant(), Json: JsonText(f.Config.Json))))
        {
            var config = group.First().Config;
            var differences = Effective([config.Ruleset is null ? [] : ReadRuleset(config.Ruleset, null)])
                .Where(p => !severities.TryGetValue(p.Key, out var s) || s != p.Value)
                .ToDictionary(p => p.Key, p => p.Value);
            var own = config.Json is not null && JsonText(config.Json) != JsonText(json) ? ReadSettings(config.Json) : null;
            if (differences.Count == 0 && own is null)
            {
                continue;
            }

            // The group's folders, widened up to where another group's project would be inside.
            var inGroup = group.Select(f => f.Folder).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uses = firstPerFolder.ToDictionary(f => Path.GetFullPath(Path.Combine(root, f.Folder, "x.csproj")), f => inGroup.Contains(f.Folder), StringComparer.OrdinalIgnoreCase);
            var covered = Cover(uses, true, root) ?? [.. inGroup.Order(StringComparer.OrdinalIgnoreCase)];
            var others = Cover(uses, false, root) ?? [];
            projectScopes.Add(new Scope(".editorconfig", SectionFor(covered, others), differences, own));
        }

        // Only where StyleCop ran (owner's decision 2026-10-10): the settings there, with the scopes that cover all of it.
        var allScopes = new List<Scope>([.. projectScopes, .. scopes]);
        var folders = Cover(projects.Uses, true, root);
        var without = folders is null ? [] : Cover(projects.Uses, false, root) ?? [];

        // Code in folders without a project of their own, compiled by projects elsewhere, goes with StyleCop's folders (Splat's
        // src/Polyfills, linked into its StyleSharp projects, was left out of the section and got StyleBro's defaults).
        // ponytail: not which projects compile it; a projectless folder below a folder without StyleCop stays out.
        if (folders is not null)
        {
            var projectFolders = files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool Under(string path, string folder) => folder == "." || path.Equals(folder, StringComparison.OrdinalIgnoreCase) || path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
            var projectless = files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/'))
                .Where(f => f != "." && !folders.Concat(without).Any(c => Under(f, c)) && !projectFolders.Any(p => p != "." && Under(f, p)))
                .Select(f => f.Split('/')[0] is var top && !projectFolders.Any(p => Under(p, top)) ? top : f);
            folders = Migration.Topmost([.. folders, .. projectless]);
        }

        if (folders is not null)
        {
            allScopes = InStyleCopFolders(allScopes, folders, without, severities);
            foreach (var id in old is null ? [] : severities.Keys.Where(id => !old.Contains(id) && !IsStyleSharpId(id)).ToList())
            {
                severities[id] = Severity.None;
            }
        }

        var copier = msbuild.FirstOrDefault(m => CopiesEditorConfig.IsMatch(WithoutComments(m.Text))).File;
        string Relative(string file) => Path.GetRelativePath(root, file).Replace('\\', '/');

        // StyleSharp's SST rules count as the StyleCop rules they stand for, its options as stylecop.json's settings.
        var styleSharp = styleSharpReferenced
            ? new StyleSharpUse(
                styleSharpVersions.Select(v => v.Value).OfType<string>().Order(StringComparer.Ordinal).FirstOrDefault() ?? recordedStyleSharp,
                ReadStyleSharpOptions(root, files),
                styleCopReferenced)
            : null;
        IReadOnlyDictionary<string, Severity>? raw = null;
        if (styleSharp is not null)
        {
            raw = severities;
            severities = Translate(severities, styleSharp);
            settings = styleSharp.WithStyleCop ? settings : styleSharp.Settings();
            if (styleSharp.WithStyleCop)
            {
                warnings.Add("Both StyleCop.Analyzers and StyleSharp.Analyzers are referenced: a StyleCop rule counts as on when either enforces it (StyleSharp's SST rules read as the StyleCop rules they stand for), and stylecop.json's settings are used.");
            }
        }

        return new StyleCopSetup(severities, settings, sources, allScopes, version, documentationParsed)
        {
            StyleSharp = styleSharp,
            Raw = raw,
            Warnings = warnings,
            EditorConfigCopiedBy = copier is null ? null : Path.GetRelativePath(root, copier),
            Folders = folders,
            FoldersWithout = without,
            ReferencedIn = projects.ReferencedIn.Select(Relative).ToList(),
            VersionRecorded = recorded is not null,
            SettingsRecorded = settingsRecorded,
            UnresolvedVersion = recorded is null ? unresolved : null,
            RemovedIn = projects.RemovedIn.Select(Relative).ToList(),
            ReferencedOutside = projects.ReferencedOutside.Select(Relative).ToList(),
            NotInstalled = !installed,
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
            else if (Kind(file) == "ruleset" && Regex.IsMatch(Migration.ReadOrNull(file) ?? string.Empty, @"\bId\s*=\s*""S[AX]\d{4}""", RegexOptions.IgnoreCase))
            {
                evidence.Add($"{relative} (StyleCop rules)");
            }
            else if (Kind(file) == "ruleset" && Regex.IsMatch(Migration.ReadOrNull(file) ?? string.Empty, @"\bId\s*=\s*""SST\d{4}""", RegexOptions.IgnoreCase))
            {
                evidence.Add($"{relative} (StyleSharp rules)");
            }
            else if (Kind(file) is "editorconfig" or "globalconfig"
                && (Migration.ReadOrNull(file) ?? string.Empty).Split('\n').Any(l => Regex.IsMatch(l, @"^\s*(dotnet_diagnostic\.S[AX]\d{4}\b|dotnet_analyzer_diagnostic\.category-StyleCop)", RegexOptions.IgnoreCase)))
            {
                evidence.Add($"{relative} (StyleCop rule severities)");
            }
            else if (Kind(file) is "editorconfig" or "globalconfig"
                && (Migration.ReadOrNull(file) ?? string.Empty).Split('\n').Any(l => Regex.IsMatch(l, @"^\s*(dotnet_diagnostic\.SST\d{4}\b|stylesharp[._])", RegexOptions.IgnoreCase)))
            {
                evidence.Add($"{relative} (StyleSharp rule severities or settings)");
            }
            else if (IsMSBuild(file) && StyleCopPackage.IsMatch(WithoutComments(Migration.ReadOrNull(file) ?? string.Empty)))
            {
                evidence.Add($"{relative} (StyleCop.Analyzers package)");
            }
            else if (IsMSBuild(file) && StyleSharpReference.IsMatch(WithoutComments(Migration.ReadOrNull(file) ?? string.Empty)))
            {
                evidence.Add($"{relative} (StyleSharp.Analyzers package)");
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
    /// folders with their own '.git' file or folder): their settings and code belong to another repository. Also skipped:
    /// an extracted NuGet package (a folder with its .nupkg), where a nuget.config's packages folder or packages.config's
    /// packages/ is inside the repository (trial 31: Hangfire's '%USERPROFILE%/.nuget/TrustedPackages', not expanded off
    /// Windows, gave init a "vendored" Microsoft.NET.Test.Sdk).
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(string root) => EnumerateFiles(root, ownRoot: false);

    /// <summary>
    /// <see cref="EnumerateFiles(string)"/>; with <paramref name="ownRoot"/>, also without the folders below the root whose
    /// .editorconfig says 'root = true' (<see cref="EditorConfigRoots"/>): the root's settings don't reach their files.
    /// </summary>
    public static IEnumerable<string> EnumerateFiles(string root, bool ownRoot)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            var files = Directory.GetFiles(directory);
            if (files.Any(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(child);
                if (name is not ("bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts") && !IsNestedRepository(child) && !(ownRoot && IsEditorConfigRoot(child)))
                {
                    pending.Push(child);
                }
            }
        }
    }

    /// <summary>
    /// The folders below the root (relative, '/'-separated, sorted) whose .editorconfig says 'root = true': the lookup for
    /// their files stops there, so nothing the root .editorconfig sets reaches them. init and the migration give each one a
    /// block of its own, judged from its own code (CleanArchitecture's sample/ and MinimalClean/: the root's block never
    /// reached them, and formatting sample/ left 41 files with mixed line endings and rewrote its '== null' checks).
    /// </summary>
    public static List<string> EditorConfigRoots(string root)
    {
        var top = Path.GetFullPath(root).TrimEnd('\\', '/');
        return EnumerateFiles(root)
            .Where(f => Path.GetFileName(f) == ".editorconfig" && Path.GetDirectoryName(Path.GetFullPath(f)) is { } folder
                && !string.Equals(folder, top, StringComparison.OrdinalIgnoreCase) && IsEditorConfigRoot(folder))
            .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Whether a folder's .editorconfig says 'root = true' (in its preamble, before the first section).</summary>
    public static bool IsEditorConfigRoot(string directory)
    {
        var path = Path.Combine(directory, ".editorconfig");
        if (!File.Exists(path))
        {
            return false;
        }

        foreach (var line in File.ReadLines(path).Select(l => l.Trim()))
        {
            if (line.StartsWith('['))
            {
                return false;
            }

            if (Regex.IsMatch(line, @"^root\s*=\s*true$", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// The path with its file name as the file system spells it ('Directory.build.props' in realworlddotnet), or as given when
    /// the file doesn't exist: messages and edits then name the file the repository has.
    /// </summary>
    public static string OnDisk(string path)
    {
        var folder = Path.GetDirectoryName(path);
        return folder is not null && Directory.Exists(folder) ? Directory.EnumerateFiles(folder, Path.GetFileName(path)).FirstOrDefault() ?? path : path;
    }

    /// <summary>A .csproj, .props or .targets file.</summary>
    public static bool IsMSBuild(string file)
    {
        return file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether StyleCop.Analyzers runs anywhere: false when the repository has projects and none references it (evaluated,
    /// <see cref="Evaluate"/>), and no earlier --write recorded a version (after the package swap it's gone on purpose).
    /// Its settings are leftovers then (Spectre.Console kept 'dotnet_diagnostic.sa1101.severity = none' lines after dropping it).
    /// </summary>
    public static bool IsInstalled(string root, Func<string, bool?>? evaluate = null) =>
        Migration.RecordedVersion(root) is not null || Migration.RecordedVersion(root, Migration.StyleSharpVersionPrefix) is not null || Evaluate(root, Projects(root, EnumerateFiles(root).ToList()).Uses, evaluate ?? EvaluateStyleCop, []);

    public bool IsOn(string id) => Severities.TryGetValue(id, out var severity) && severity >= Severity.Suggestion;

    /// <summary>The setup as seen inside a scope: the base severities with the scope's on top.</summary>
    public StyleCopSetup For(Scope scope)
    {
        var severities = (Raw ?? Severities).ToDictionary(p => p.Key, p => p.Value);
        foreach (var (id, severity) in scope.Severities)
        {
            severities[id] = severity;
        }

        return new StyleCopSetup(StyleSharp is null ? severities : Translate(severities, StyleSharp), scope.Settings ?? Settings, Sources, [], Version, DocumentationParsed)
        {
            StyleSharp = StyleSharp,
            Raw = StyleSharp is null ? null : severities,
        };
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
    /// The severities with every StyleCop rule (SA and SX ids) read from StyleSharp's SST rules (<see cref="StyleSharpRules"/>):
    /// one stands for several (SA1117's list kinds) at the weakest of them, a StyleCop rule no SST rule stands for is off.
    /// SST1117 is SA1101 or StyleCop's SX1101 as stylesharp.instance_member_qualification says (StyleSharp omits 'this.' by
    /// default); SST1633 only checks a header with file_header_template set. With StyleCop running too (<see
    /// cref="StyleSharpUse.WithStyleCop"/>), a rule is on when either enforces it.
    /// </summary>
    internal static Dictionary<string, Severity> Translate(IReadOnlyDictionary<string, Severity> raw, StyleSharpUse styleSharp)
    {
        var fromStyleSharp = new Dictionary<string, Severity>(StringComparer.Ordinal);
        foreach (var (sst, _, defaultSeverity, _, styleCop) in StyleSharpRules)
        {
            var severity = raw.TryGetValue(sst, out var set) ? set : defaultSeverity;
            foreach (var id in StandsFor(sst, styleCop, styleSharp))
            {
                fromStyleSharp[id] = fromStyleSharp.TryGetValue(id, out var before) && before < severity ? before : severity;
            }
        }

        var result = raw.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        foreach (var (id, _, _, _) in Rules)
        {
            var mine = fromStyleSharp.TryGetValue(id, out var s) ? s : Severity.None;
            result[id] = styleSharp.WithStyleCop && raw.TryGetValue(id, out var styleCopSeverity) && styleCopSeverity > mine ? styleCopSeverity : mine;
        }

        return result;
    }

    /// <summary>
    /// The StyleCop rules an SST rule stands for here (<paramref name="styleCop"/>: the mapping's): SST1117 is SA1101 with
    /// stylesharp.instance_member_qualification = require_this, else SX1101; SST1633 checks nothing without file_header_template.
    /// </summary>
    internal static IReadOnlyList<string> StandsFor(string sst, IReadOnlyList<string> styleCop, StyleSharpUse styleSharp) => sst switch
    {
        "SST1117" => [styleSharp.Option("instance_member_qualification", sst) is { } q && q.Equals("require_this", StringComparison.OrdinalIgnoreCase) ? "SA1101" : "SX1101"],
        "SST1633" => styleSharp.Options.TryGetValue("file_header_template", out var template) && template.Length > 0 ? styleCop : [],
        _ => styleCop,
    };

    /// <summary>Whether a project (full path) is in a folder of the block's list ('a/b/', or a 'a/b*' pattern).</summary>
    internal static bool InFolder(string root, string project, string folder)
    {
        var path = Path.GetRelativePath(root, Path.GetDirectoryName(project)!).Replace('\\', '/') + "/";
        var trimmed = folder.Trim().TrimEnd('/');
        return trimmed.EndsWith('*')
            ? path.StartsWith(trimmed.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)
            : path.StartsWith(trimmed + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Each project (full path) under the root and whether StyleCop runs in it: a StyleCop.Analyzers reference in the project,
    /// the Directory.Build.props/.targets or Directory.Packages.props it gets, or what those import, and no Remove item for it.
    /// Also the repository's own files that reference it and that remove it, and the files outside them that reference it
    /// (a git submodule's: SixLabors' shared-infrastructure props, which this repository can't edit).
    /// </summary>
    internal static (Dictionary<string, bool> Uses, List<string> ReferencedIn, List<string> RemovedIn, List<string> ReferencedOutside) Projects(string root, IReadOnlyCollection<string> files)
    {
        // ponytail: text-level, like FolderRulesets: Conditions are ignored (Evaluate settles those), a Remove anywhere in the chain wins.
        var own = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        var uses = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var referenced = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var removed = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var outside = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            var chain = Chain(project, root);
            var references = chain.Where(f => StyleCopReference.IsMatch(WithoutComments(Migration.ReadOrNull(f) ?? string.Empty))).ToList();
            var removes = chain.Where(f => StyleCopRemove.IsMatch(WithoutComments(Migration.ReadOrNull(f) ?? string.Empty))).ToList();
            uses[project] = references.Count > 0 && removes.Count == 0;
            referenced.UnionWith(references.Where(own.Contains));
            outside.UnionWith(references.Where(f => !own.Contains(f)));
            removed.UnionWith(removes.Where(own.Contains));
        }

        return (uses, referenced.ToList(), removed.ToList(), outside.ToList());
    }

    /// <summary>
    /// Settles <paramref name="uses"/> (from <see cref="Projects"/>, text-level) with MSBuild: every project when there are
    /// several (the text is only the fallback for a project MSBuild can't evaluate); with one, where the text can't: a project
    /// whose StyleCop reference sits under a Condition (DynamicData's '&lt;ItemGroup Condition="!$(IsTestProject) AND
    /// !$(IsBenchmarkProject)"&gt;' counted its tests, and 360 test files changed where StyleCop never ran), and, when no
    /// project references it in the text, that one (a reference the text can't see, before its settings count as
    /// leftovers). A project MSBuild can't evaluate keeps the text's answer, with a warning. Returns whether any project
    /// uses StyleCop (true without projects: nothing tells otherwise).
    /// </summary>
    internal static bool Evaluate(string root, Dictionary<string, bool> uses, Func<string, bool?> evaluate, List<string> warnings)
    {
        if (uses.Count == 0)
        {
            return true;
        }

        // With several projects MSBuild decides for every one: the text keeps missing ways of importing a file (Nerdbank.Streams'
        // test/Directory.Packages.props imports the root's, which has StyleCop, as 'GetPathOfFileAbove($(MSBuildThisFile), ...)':
        // test/ looked like it had no StyleCop, and kept 874 warnings that contradict it). ~1-2 s per project, in parallel, and
        // format reuses it (FormatCommand.EvaluateProject). One project: only where the text can't tell.
        var check = uses.Count > 1 || !uses.Values.Contains(true)
            ? uses.Keys.ToList()
            : uses.Where(p => p.Value && Chain(p.Key, root).Any(f => ConditionalReference(Migration.ReadOrNull(f) ?? string.Empty))).Select(p => p.Key).ToList();
        var results = check.AsParallel().Select(p => (Project: p, Uses: evaluate(p))).ToList();
        foreach (var (project, value) in results)
        {
            if (value is { } evaluated)
            {
                uses[project] = evaluated;
            }
        }

        var failed = results.Where(r => r.Uses is null).Select(r => Path.GetRelativePath(root, r.Project).Replace('\\', '/')).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (failed.Count > 0)
        {
            warnings.Add($"MSBuild couldn't evaluate {string.Join(", ", failed.Take(5))}{(failed.Count > 5 ? $" and {failed.Count - 5} more" : string.Empty)}: whether StyleCop runs there was read from the project files' text (Conditions ignored, and an import this reader can't follow is missed).");
        }

        return uses.Values.Contains(true);
    }

    /// <summary>
    /// Whether StyleCop.Analyzers (or StyleSharp) is among a project's evaluated PackageReference, GlobalPackageReference or
    /// Analyzer items ('dotnet msbuild -getItem', Conditions applied), or null when MSBuild can't evaluate it. A multi-targeted
    /// project's outer evaluation has no TargetFramework, so a reference under a framework Condition is checked per framework
    /// (<see cref="UsesStyleCop(string, Func{string, string})"/>).
    /// </summary>
    internal static bool? EvaluateStyleCop(string project)
    {
        var fullPath = Path.GetFullPath(project);
        try
        {
            return UsesStyleCop(FormatCommand.EvaluateProject(fullPath), framework => FrameworkEvaluations.GetOrAdd(fullPath + "|" + framework, _ => new Lazy<string>(() => FormatCommand.Capture(
                Path.GetDirectoryName(fullPath)!,
                "msbuild",
                fullPath,
                $"-property:TargetFramework={framework}",
                "-getItem:PackageReference",
                "-getItem:GlobalPackageReference",
                "-getItem:Analyzer",
                "-nologo"))).Value);
        }
        catch (Exception e) when (e is ToolException or JsonException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether an evaluation ('dotnet msbuild -getItem' JSON) has StyleCop's items; when not and it lists several
    /// TargetFrameworks, whether any framework's evaluation (<paramref name="evaluateFramework"/>, in parallel) has them
    /// (fluentassertions: StyleCop.Analyzers under '$(TargetFramework)' == 'net6.0', so Src/ looked like it had no StyleCop,
    /// format changed nothing and the build failed with 2,900 errors).
    /// </summary>
    internal static bool UsesStyleCop(string evaluation, Func<string, string> evaluateFramework)
    {
        using var document = JsonDocument.Parse(evaluation);
        if (HasStyleCopItem(document.RootElement))
        {
            return true;
        }

        var frameworks = document.RootElement.TryGetProperty("Properties", out var properties) && properties.TryGetProperty("TargetFrameworks", out var value)
            ? (value.GetString() ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
        return frameworks.Length > 1 && frameworks.AsParallel().Any(framework =>
        {
            using var inner = JsonDocument.Parse(evaluateFramework(framework));
            return HasStyleCopItem(inner.RootElement);
        });
    }

    /// <summary>Whether an evaluated item's Identity is StyleCop's (or StyleSharp's) package, or an analyzer DLL from it.</summary>
    internal static bool IsStyleCopItem(string identity) =>
        Regex.IsMatch(identity, @"^(StyleCop\.Analyzers(\.Unstable)?|StyleSharp\.Analyzers)$|(StyleCop|StyleSharp)\.Analyzers[^\\/]*[\\/].*\.dll$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Whether an MSBuild file's StyleCop reference depends on a Condition (on the item, an enclosing ItemGroup, or a
    /// Choose's When/Otherwise). Unparsable XML counts as conditional: MSBuild decides.
    /// </summary>
    internal static bool ConditionalReference(string text)
    {
        if (!StyleCopReference.IsMatch(WithoutComments(text)))
        {
            return false;
        }

        try
        {
            return XDocument.Parse(text).Descendants()
                .Where(e => e.Name.LocalName is "PackageReference" or "GlobalPackageReference" or "Analyzer"
                    && ((string?)e.Attribute("Include") is { } include && (include.Contains("StyleCop.Analyzers", StringComparison.OrdinalIgnoreCase) || include.Contains("StyleSharp.Analyzers", StringComparison.OrdinalIgnoreCase))))
                .Any(e => e.AncestorsAndSelf().Any(a => a.Attribute("Condition") is not null || a.Name.LocalName is "When" or "Otherwise"));
        }
        catch (System.Xml.XmlException)
        {
            return true;
        }
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
            foreach (Match match in Reference.Matches(WithoutComments(Migration.ReadOrNull(file) ?? string.Empty)))
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
        Reference.Matches(WithoutComments(Migration.ReadOrNull(file) ?? string.Empty))
            .Where(m => m.Groups["import"].Success)
            .Select(m => Resolve(m.Groups["import"].Value, file, root))
            .OfType<string>()
            .Where(IsMSBuild);

    /// <summary>
    /// The ruleset and stylecop.json a project gets: its nearest Directory.Build.props, the project file and its nearest
    /// Directory.Build.targets, each with what it imports, in MSBuild's order. The last CodeAnalysisRuleSet counts
    /// ("'$(CodeAnalysisRuleSet)' == ''" conditions respected; a relative value is the project's), the first stylecop.json
    /// additional file. Paths are full; null when there's none (or it can't be resolved).
    /// </summary>
    internal static ProjectStyleCop ProjectConfig(string project, string root)
    {
        // ponytail: text-level evaluation (no PropertyGroup conditions, no other properties); an unknown value counts as none.
        string? ruleset = null;
        string? json = null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rootFolder = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? Nearest(string name)
        {
            for (var folder = Path.GetDirectoryName(project); folder is not null && folder.Length >= rootFolder.Length; folder = Path.GetDirectoryName(folder))
            {
                if (File.Exists(Path.Combine(folder, name)))
                {
                    return Path.Combine(folder, name);
                }
            }

            return null;
        }

        foreach (var file in new[] { Nearest("Directory.Build.props"), project, Nearest("Directory.Build.targets") }.OfType<string>())
        {
            Evaluate(file, 0);
        }

        return new ProjectStyleCop(ruleset, json);

        // MSBuild resolves a relative path in a property or item against the project, wherever it's written.
        string? FromProject(string value, string file) =>
            (value.Contains("$(MSBuildThisFileDirectory)", StringComparison.OrdinalIgnoreCase) ? null : Resolve(value, project, root)) ?? Resolve(value, file, root);

        void Evaluate(string file, int depth)
        {
            if (depth > 16 || !seen.Add(file))
            {
                return;
            }

            foreach (Match match in Reference.Matches(WithoutComments(Migration.ReadOrNull(file) ?? string.Empty)))
            {
                if (match.Groups["import"].Success)
                {
                    if (Resolve(match.Groups["import"].Value, file, root) is { } imported && IsMSBuild(imported))
                    {
                        Evaluate(imported, depth + 1);
                    }
                }
                else if (match.Groups["ruleset"].Success && (ruleset is null || !match.Groups["condition"].Value.Contains("$(CodeAnalysisRuleSet)", StringComparison.OrdinalIgnoreCase)))
                {
                    ruleset = FromProject(match.Groups["ruleset"].Value, file);
                }
                else if (match.Groups["additional"].Success && json is null && Path.GetFileName(match.Groups["additional"].Value.Trim().Replace('\\', '/')).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
                {
                    json = FromProject(match.Groups["additional"].Value, file);
                }
            }
        }
    }

    /// <summary>
    /// The rules a StyleCop version before 1.2 has (its DLL's inventory), or null for 1.2 and later: 1.0.x lacks SA1135,
    /// SA1136, SA1137, SA1139, SA1314 and SA1413 too (xbox-live-developer-tools, 1.0.2: 94 findings in StyleCop-clean code).
    /// </summary>
    /// <summary>
    /// A version with its '$(Name)' properties replaced by the values the MSBuild files (<paramref name="texts"/>, without
    /// comments) give them ('&lt;StyleCopVersion&gt;1.2.0-beta.556&lt;/StyleCopVersion&gt;'), or null when one isn't defined.
    /// </summary>
    internal static string? ResolveProperties(string value, IReadOnlyList<string> texts)
    {
        // ponytail: the first definition wins, conditions aren't evaluated (MSBuild's evaluation would be a process per repository).
        for (var depth = 0; depth < 8 && value.Contains("$(", StringComparison.Ordinal); depth++)
        {
            value = Regex.Replace(value, @"\$\((?<name>[\w.-]+)\)", m => texts
                .SelectMany(t => Regex.Matches(t, $@"<{Regex.Escape(m.Groups["name"].Value)}(?:\s[^>]*)?>\s*(?<v>[^<]*?)\s*</{Regex.Escape(m.Groups["name"].Value)}\s*>"))
                .FirstOrDefault(d => d.Success && d.Groups["v"].Value.Length > 0)?.Groups["v"].Value ?? m.Value);
        }

        return value.Contains("$(", StringComparison.Ordinal) ? null : value;
    }

    internal static HashSet<string>? OldRules(string? version) =>
        version is null ? null
        : version.StartsWith("1.0.", StringComparison.Ordinal) ? LoadIds("StyleCopRules-1.0.2.csv")
        : version.StartsWith("1.1.", StringComparison.Ordinal) ? LoadIds("StyleCopRules-1.1.118.csv")
        : null;

    /// <summary>
    /// The global configs packages add to the projects (<paramref name="read"/>: a project's, null without its restore output),
    /// in order, without the repository's own files (read with the others). A project without restore output whose MSBuild
    /// files reference other packages gets a warning: a package's global config can only be seen once it is restored.
    /// </summary>
    internal static List<string> PackageGlobalConfigs(string root, IEnumerable<string> projects, Func<string, IReadOnlyList<string>?> read, List<string> warnings)
    {
        var result = new List<string>();
        var unrestored = new List<string>();
        foreach (var project in projects.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (read(project) is not { } found)
            {
                if (Chain(project, root).Any(f => OtherPackage.IsMatch(WithoutComments(Migration.ReadOrNull(f) ?? string.Empty))))
                {
                    unrestored.Add(Path.GetRelativePath(root, project).Replace('\\', '/'));
                }

                continue;
            }

            result.AddRange(found.Where(f => !InRepository(root, f) && !result.Contains(f, StringComparer.OrdinalIgnoreCase)));
        }

        if (unrestored.Count > 0)
        {
            warnings.Add($"{NotRestored} {string.Join(", ", unrestored.Take(5))}{(unrestored.Count > 5 ? $" and {unrestored.Count - 5} more" : string.Empty)}. A package can bring a global config that sets StyleCop's rules (GlobalAnalyzerConfigFiles), and it is read from the restore: run 'dotnet restore', then this again.");
        }

        return result;
    }

    /// <summary>
    /// The global configs a project's evaluation lists (<c>GlobalAnalyzerConfigFiles</c>, full paths that exist), or null when
    /// it has no restore output yet (a package's build props, which add them, are imported from there) or MSBuild fails.
    /// </summary>
    internal static IReadOnlyList<string>? ReadPackageGlobalConfigs(string project, string root)
    {
        if (!Restored(project, root))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(FormatCommand.EvaluateProject(Path.GetFullPath(project)));
            return json.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty("GlobalAnalyzerConfigFiles", out var configs)
                ? configs.EnumerateArray()
                    .Select(c => c.TryGetProperty("FullPath", out var full) ? full.GetString() : c.GetProperty("Identity").GetString())
                    .OfType<string>()
                    .Select(Path.GetFullPath)
                    .Where(File.Exists)
                    .ToList()
                : [];
        }
        catch (Exception e) when (e is ToolException or JsonException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether a project has restore output: its <c>&lt;name&gt;.nuget.g.props</c> in obj/ next to it, or in an artifacts
    /// folder's obj/&lt;project&gt;/ (UseArtifactsOutput, Pidgin) at or above it under the root.
    /// </summary>
    internal static bool Restored(string project, string root)
    {
        var name = Path.GetFileName(project) + ".nuget.g.props";
        var folder = Path.GetDirectoryName(Path.GetFullPath(project))!;
        if (File.Exists(Path.Combine(folder, "obj", name)))
        {
            return true;
        }

        var top = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var above = folder; above is not null && above.Length >= top.Length; above = Path.GetDirectoryName(above))
        {
            if (File.Exists(Path.Combine(above, "artifacts", "obj", Path.GetFileNameWithoutExtension(project), name)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasStyleCopItem(JsonElement evaluation) =>
        evaluation.TryGetProperty("Items", out var items)
        && items.EnumerateObject().Where(i => i.Name is "PackageReference" or "GlobalPackageReference" or "Analyzer").SelectMany(i => i.Value.EnumerateArray())
            .Any(i => i.TryGetProperty("Identity", out var identity) && IsStyleCopItem(identity.GetString() ?? string.Empty));

    /// <summary>Whether a file is the repository's own (under the root, not in an extracted package there).</summary>
    private static bool InRepository(string root, string file)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(file));
        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative) && !FormatCommand.IsPackageFile(file);
    }

    /// <summary>
    /// A project's MSBuild files as the text reads them: the project, the Directory.Build.props/.targets and
    /// Directory.Packages.props it gets (nearest, under the root), and what those import.
    /// </summary>
    private static List<string> Chain(string project, string root)
    {
        var rootFolder = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var chain = new List<string> { project };
        foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
        {
            for (var folder = Path.GetDirectoryName(project); folder is not null && folder.Length >= rootFolder.Length; folder = Path.GetDirectoryName(folder))
            {
                if (File.Exists(Path.Combine(folder, name)))
                {
                    chain.Add(OnDisk(Path.Combine(folder, name)));
                    break;
                }
            }
        }

        for (var i = 0; i < chain.Count && i < 64; i++)
        {
            foreach (Match match in Reference.Matches(WithoutComments(Migration.ReadOrNull(chain[i]) ?? string.Empty)))
            {
                if (match.Groups["import"].Success && Resolve(match.Groups["import"].Value, chain[i], root) is { } imported && IsMSBuild(imported)
                    && !chain.Contains(imported, StringComparer.OrdinalIgnoreCase))
                {
                    chain.Add(imported);
                }
            }
        }

        return chain;
    }

    /// <summary>Whether an .editorconfig section can apply to C# files at all ('[tests/**.cs]', '[*Tests.cs]', '[{src/tests/**/*.cs,tests/**/*.cs}]').</summary>
    private static bool MayApplyToCSharp(string section)
    {
        return section.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(section, @"[{,]\s*cs\s*[,}]|\.cs\s*[,}]")
            || section.EndsWith("*", StringComparison.Ordinal);
    }

    /// <summary>
    /// The StyleCop severities each section of a .editorconfig or .globalconfig sets (a global config is one section):
    /// per rule, or per StyleCop category. Sections that can't apply to C# files are left out. <c>Bulk</c> holds the
    /// rules set only by a category or 'dotnet_analyzer_diagnostic.severity': in the compiler those don't override a
    /// rule's own entry in a ruleset or another config. <c>Sonar</c>: the Sonar rules' own entries.
    /// </summary>
    private static List<(string Section, Dictionary<string, Severity> Severities, HashSet<string> Bulk, Dictionary<string, Severity> Sonar)> ReadConfig(string file, bool global)
    {
        var sections = new List<(string Section, Dictionary<string, Severity> ByRule, Dictionary<string, Severity> ByCategory, Dictionary<string, Severity> Sonar)>();
        (string Section, Dictionary<string, Severity> ByRule, Dictionary<string, Severity> ByCategory, Dictionary<string, Severity> Sonar)? current =
            global ? (string.Empty, new(StringComparer.OrdinalIgnoreCase), new(), new(StringComparer.Ordinal)) : null;
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
                current = global || MayApplyToCSharp(section) ? (section, new(StringComparer.OrdinalIgnoreCase), new(), new(StringComparer.Ordinal)) : null;
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
            else if (SonarKey.Match(key) is { Success: true } sonar)
            {
                target.Sonar[sonar.Groups[1].Value.ToUpperInvariant()] = severity;
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

        var result = new List<(string, Dictionary<string, Severity>, HashSet<string>, Dictionary<string, Severity>)>();
        foreach (var (section, byRule, byCategory, sonarRules) in sections)
        {
            var severities = new Dictionary<string, Severity>();
            var bulk = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (id, category, defaultSeverity) in ReadRules)
            {
                // A rule's own entry, else its category's, else 'dotnet_analyzer_diagnostic.severity' (every rule; a folder of
                // vendored code often sets it to none). Like in the compiler (TryGetSeverityFromBulkConfiguration), the two bulk
                // entries only apply to rules that are on by default: tfplan2md's 'category-StyleCop.CSharp.NamingRules.severity
                // = warning' never turned SA1305 on, and reading it so turned BRO1310 on (6 files renamed).
                if (byRule.TryGetValue(id, out var severity)
                    || (defaultSeverity > Severity.None && (byCategory.TryGetValue(category, out severity) || byCategory.TryGetValue(AllRules, out severity))))
                {
                    severities[id] = severity;
                    if (!byRule.ContainsKey(id))
                    {
                        bulk.Add(id);
                    }
                }
            }

            if (severities.Count > 0 || sonarRules.Count > 0)
            {
                result.Add((section, severities, bulk, sonarRules));
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
                if (id is not null && (id.StartsWith("SA", StringComparison.OrdinalIgnoreCase) || id.StartsWith("SX", StringComparison.OrdinalIgnoreCase) || IsStyleSharpId(id)) && action is not null && ParseSeverity(action) is { } severity)
                {
                    result[id.ToUpperInvariant()] = severity;
                }
            }
        }
        catch (System.Xml.XmlException)
        {
            // Not a ruleset after all.
        }

        return result;
    }

    /// <summary>stylecop.json's 'settings' object, or null when it has none.</summary>
    private static JsonElement? ReadSettings(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        return document.RootElement.TryGetProperty("settings", out var element) ? element.Clone() : null;
    }

    /// <summary>
    /// An MSBuild path as an existing file under the root, or null: $(MSBuildThisFileDirectory) and GetPathOfFileAbove are
    /// filled in, a relative path is taken from the file's folder, anything with other properties or wildcards is unknown.
    /// </summary>
    private static string? Resolve(string value, string file, string root)
    {
        var directory = Path.GetDirectoryName(file)! + Path.DirectorySeparatorChar;
        value = value.Trim().Replace("$(MSBuildThisFileDirectory)", directory, StringComparison.OrdinalIgnoreCase);

        // The usual ways a nested Directory.Build.props imports its parent's: a property the same file defines, the SDK's
        // name for the file, Path.Combine (Orleans: $(_ParentDirectoryBuildPropsPath) = Combine('..', '$(_DirectoryBuildPropsFile)');
        // read as a path it never resolved, and the preview added the StyleBro reference to src/, test/ and the root: NU1504).
        // ponytail: one file's unconditional definitions, not MSBuild's evaluation.
        if (value.Contains("$(", StringComparison.Ordinal))
        {
            var text = WithoutComments(File.ReadAllText(file));
            for (var i = 0; i < 4 && Regex.IsMatch(value, @"\$\(\w+\)"); i++)
            {
                // MSBuildProjectExtension: openmcdf imports 'Directory$(MSBuildProjectExtension).props' (StyleBro is C# only).
                // MSBuildThisFile: the Library.Template's 'GetPathOfFileAbove($(MSBuildThisFile), $(MSBuildThisFileDirectory)..)'.
                value = Regex.Replace(value, @"\$\((\w+)\)", m => m.Groups[1].Value.Equals("_DirectoryBuildPropsFile", StringComparison.OrdinalIgnoreCase) ? "Directory.Build.props"
                    : m.Groups[1].Value.Equals("MSBuildThisFile", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(file)
                    : m.Groups[1].Value.Equals("MSBuildProjectExtension", StringComparison.OrdinalIgnoreCase) ? ".csproj"
                    : Regex.Match(text, $@"<{m.Groups[1].Value}\b[^>]*>([^<]*)</{m.Groups[1].Value}\s*>") is { Success: true } defined ? defined.Groups[1].Value.Trim()
                    : m.Value);
            }

            value = Regex.Replace(value, @"^\$\(\[System\.IO\.Path\]::Combine\(\s*'([^']*)'\s*,\s*'([^']*)'\s*\)\)$", m => Path.Combine(m.Groups[1].Value, m.Groups[2].Value), RegexOptions.IgnoreCase);
        }

        // Quoted or not (Nerdbank.Streams' test/Directory.Packages.props: its import of the root file, with StyleCop's
        // GlobalPackageReference, wasn't followed, and test/ looked like it had no StyleCop).
        var above = Regex.Match(value, @"^\$\(\[MSBuild\]::GetPathOfFileAbove\(\s*'?([^',]+?)'?\s*(?:,\s*'?(.*?)'?)?\s*\)\)$", RegexOptions.IgnoreCase);
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
                // The base has its StyleCop severities; its Sonar ones stay a scope (SonarSetup's base is the root's).
                Overlay(severities, scope.Severities);
                if (scope.Sonar is not null)
                {
                    result.Add(scope with { Severities = new Dictionary<string, Severity>() });
                }
            }
            else if (covered.Count > 0)
            {
                result.Add(scope with { File = ".editorconfig", Section = SectionFor(covered, [.. without, .. folders.Except(covered)]) });
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

    /// <summary>Whether a rule id is StyleSharp's ('SST1309').</summary>
    private static bool IsStyleSharpId(string id) => id.StartsWith("SST", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A package's version in MSBuild files and packages.config: a PackageReference/PackageVersion (attribute or element), a
    /// packages.config entry, or an analyzer's hint path into a packages.config 'packages' folder.
    /// </summary>
    private static Regex VersionPattern(string package) => new(
        $@"Include=""{package}""[^>]*?Version=""(?<v>[^""]+)""|Include=""{package}""[^>]*>\s*<Version>(?<v>[^<]+)</Version>|<package\b[^>]*?\bid=""{package}""[^>]*?\bversion=""(?<v>[^""]+)""|{package}\.(?<v>\d+(?:\.\d+){{2,3}}(?:-[\w.]+)?)[\\/]",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// StyleSharp's options ('stylesharp.*' keys, 'stylesharp_*' spelled with '.', and file_header_template, which SST1633 reads):
    /// from the global configs, then the root .editorconfig's sections for all C# (later wins).
    /// ponytail: repository-wide only; a sub-folder's own 'stylesharp.*' keys aren't scopes.
    /// </summary>
    private static Dictionary<string, string> ReadStyleSharpOptions(string root, IReadOnlyList<string> files)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rootConfig = Path.Combine(root, ".editorconfig");
        foreach (var file in files.Where(f => Kind(f) == "globalconfig").Order(StringComparer.OrdinalIgnoreCase).Concat(File.Exists(rootConfig) ? [rootConfig] : []))
        {
            var applies = Kind(file) == "globalconfig";
            foreach (var line in Regex.Replace(File.ReadAllText(file), $"{Migration.BeginMarker}.*?{Migration.EndMarker}", string.Empty, RegexOptions.Singleline).Split('\n').Select(l => l.Trim()))
            {
                if (line.Length > 0 && line[0] == '[')
                {
                    applies = Kind(file) == "globalconfig" || AppliesToCSharp(line.Trim('[', ']'));
                    continue;
                }

                var equals = line.IndexOf('=');
                var key = equals > 0 && !line.StartsWith('#') && !line.StartsWith(';') ? line.Substring(0, equals).Trim() : string.Empty;
                if (applies && (key.StartsWith("stylesharp", StringComparison.OrdinalIgnoreCase) || key.Equals("file_header_template", StringComparison.OrdinalIgnoreCase)))
                {
                    var name = Regex.Replace(key, "^stylesharp_", "stylesharp.", RegexOptions.IgnoreCase);
                    options[name] = line.Substring(equals + 1).Split('#', ';')[0].Trim();
                }
            }
        }

        return options;
    }

    private static IReadOnlyList<(string Id, string Category, Severity Default, string Title, IReadOnlyList<string> StyleCop)> LoadStyleSharpRules()
    {
        static List<List<string>> Rows(string resource)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"{resource} isn't embedded.");
            using var reader = new StreamReader(stream);
            reader.ReadLine();
            var rows = new List<List<string>>();
            while (reader.ReadLine() is { } line)
            {
                rows.Add(Regex.Matches(line, "\"((?:[^\"]|\"\")*)\"|([^,]+)|(?<=,)(?=,)").Select(m => m.Groups[1].Success ? m.Groups[1].Value.Replace("\"\"", "\"") : m.Value).ToList());
            }

            return rows;
        }

        // Id,Title,Category,EnabledByDefault,DefaultSeverity,... and Id,StyleCop,Note
        var mapping = Rows("StyleSharpMapping.csv").ToDictionary(r => r[0], r => (IReadOnlyList<string>)r[1].Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        return Rows("StyleSharpRules.csv")
            .Where(f => f.Count >= 5)
            .Select(f => (f[0], f[2], f[3].Equals("True", StringComparison.OrdinalIgnoreCase) ? ParseSeverity(f[4]) ?? Severity.Warning : Severity.None, f[1], mapping.TryGetValue(f[0], out var ids) ? ids : []))
            .ToList();
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
