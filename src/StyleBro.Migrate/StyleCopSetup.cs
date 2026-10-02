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
    private static readonly Regex DiagnosticKey = new(@"^dotnet_diagnostic\.(S[AX]\d{4}\w*)\.severity$", RegexOptions.IgnoreCase);
    private static readonly Regex PackageVersion = new(@"Include=""StyleCop\.Analyzers""[^>]*?Version=""([^""]+)""|Include=""StyleCop\.Analyzers""[^>]*>\s*<Version>([^<]+)</Version>", RegexOptions.IgnoreCase);
    private static readonly Regex DocumentationFile = new(@"<GenerateDocumentationFile>\s*true\s*<|<DocumentationFile>", RegexOptions.IgnoreCase);
    private static readonly Regex CategoryKey = new(@"^dotnet_analyzer_diagnostic\.category-StyleCop\.CSharp\.(\w+)\.severity$", RegexOptions.IgnoreCase);

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

    public static StyleCopSetup Read(string root)
    {
        var files = EnumerateFiles(root).ToList();
        var sources = new List<string>();
        var scopes = new List<Scope>();
        var severities = Rules.ToDictionary(r => r.Id, r => r.Default);

        // Rulesets and global configs: merged across files (strictest wins), then applied over the previous kind.
        foreach (var kind in new[] { "ruleset", "globalconfig" })
        {
            var perKind = new Dictionary<string, Severity>();
            foreach (var file in files.Where(f => Kind(f) == kind))
            {
                var found = kind == "ruleset" ? ReadRuleset(file) : Merge(ReadConfig(file, global: true).Select(s => s.Severities));
                if (found.Count == 0)
                {
                    continue;
                }

                sources.Add(Path.GetRelativePath(root, file));
                foreach (var (id, severity) in found)
                {
                    perKind[id] = perKind.TryGetValue(id, out var existing) && existing > severity ? existing : severity;
                }
            }

            Overlay(severities, perKind);
        }

        // .editorconfig: the root file's sections for all C# files are the base; everything else is a scope.
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
                    Overlay(severities, section.Severities);
                }
                else
                {
                    scopes.Add(new Scope(relative, section.Section, section.Severities));
                }
            }
        }

        JsonElement? settings = null;
        var json = files.Where(f => Path.GetFileName(f).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
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

        // Rules added in StyleCop 1.2 (SA1651, SA1141, ...) don't exist in 1.1.x: off, whatever the configs say.
        var msbuild = files.Where(IsMSBuild).Select(File.ReadAllText).ToList();
        var documentationParsed = msbuild.Any(text => DocumentationFile.IsMatch(text));
        var version = msbuild.Select(text => PackageVersion.Match(text))
            .Where(m => m.Success).Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value.Trim())
            .OrderBy(v => v, StringComparer.Ordinal).FirstOrDefault();
        if (version is not null && (version.StartsWith("1.1.", StringComparison.Ordinal) || version.StartsWith("1.0.", StringComparison.Ordinal)))
        {
            var old = LoadIds("StyleCopRules-1.1.118.csv");
            foreach (var id in severities.Keys.Where(id => !old.Contains(id)).ToList())
            {
                severities[id] = Severity.None;
            }
        }

        return new StyleCopSetup(severities, settings, sources, scopes, version, documentationParsed);
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

    /// <summary>Files in the repository, skipping build output and version control.</summary>
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
                if (name is not ("bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts"))
                {
                    pending.Push(child);
                }
            }
        }
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

    /// <summary>Whether an .editorconfig section can apply to C# files at all ('[tests/**.cs]', '[*Tests.cs]').</summary>
    private static bool MayApplyToCSharp(string section)
    {
        return section.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(section, @"[{,]\s*cs\s*[,}]")
            || section.EndsWith("*", StringComparison.Ordinal);
    }

    /// <summary>
    /// The StyleCop severities each section of a .editorconfig or .globalconfig sets (a global config is one section):
    /// per rule, or per StyleCop category. Sections that can't apply to C# files are left out.
    /// </summary>
    private static List<(string Section, Dictionary<string, Severity> Severities)> ReadConfig(string file, bool global)
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
        }

        AddCurrent();

        var result = new List<(string, Dictionary<string, Severity>)>();
        foreach (var (section, byRule, byCategory) in sections)
        {
            var severities = new Dictionary<string, Severity>();
            foreach (var (id, category, _, _) in Rules)
            {
                if (byRule.TryGetValue(id, out var severity) || byCategory.TryGetValue(category, out severity))
                {
                    severities[id] = severity;
                }
            }

            if (severities.Count > 0)
            {
                result.Add((section, severities));
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

    private static Dictionary<string, Severity> ReadRuleset(string file)
    {
        var result = new Dictionary<string, Severity>();
        try
        {
            foreach (var rule in XDocument.Load(file).Descendants("Rule"))
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

    private static Dictionary<string, Severity> Merge(IEnumerable<Dictionary<string, Severity>> sections)
    {
        var result = new Dictionary<string, Severity>();
        foreach (var section in sections)
        {
            Overlay(result, section);
        }

        return result;
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
