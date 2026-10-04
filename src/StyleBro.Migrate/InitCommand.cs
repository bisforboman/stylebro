using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate init [path] [--write] [--modernize]: puts the severities of the built-in .NET rules the preset relies
/// on into the repository's root .editorconfig. They can't live in the preset: 'dotnet format' ignores rule severities in a
/// package's global config (the build doesn't), so only .editorconfig makes it fix them. Written between the same markers
/// as 'stylebro-migrate --write', which replaces the block with settings matched to a StyleCop setup. --modernize adds a
/// second block with the SDK's modernization rules (<see cref="Modernize"/>).
/// </summary>
internal static class InitCommand
{
    /// <summary>
    /// What multi-targeted repositories need to know: plain 'dotnet format' crashes on IDE0055's fix there (Roslyn's
    /// linked-file merge, measured on Serilog and Newtonsoft.Json without StyleBro); 'stylebro-migrate format' doesn't.
    /// </summary>
    public const string MultiTargetedHint =
        "Run 'stylebro-migrate format' instead of 'dotnet format' here: with IDE0055 on, 'dotnet format' crashes on multi-targeted\n"
        + "projects (Roslyn can't merge the target frameworks' copies of a file). The command runs it once per target framework.";

    /// <summary>The start marker of the --modernize block (its own block, so it survives 'stylebro-migrate --write').</summary>
    public const string ModernizeBegin = "# BEGIN stylebro-modernize";

    /// <summary>The end marker of the --modernize block.</summary>
    public const string ModernizeEnd = "# END stylebro-modernize";

    public static int Run(string[] args)
    {
        var write = args.Contains("--write");
        var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        var multiTargeted = MultiTargetedProjects(root).ToList();
        var block = Block();
        var modernize = args.Contains("--modernize") ? Modernize(root, multiTargeted) : (Block: null, Notes: new List<string>());
        var path = Path.Combine(root, ".editorconfig");
        if (multiTargeted.Count > 0)
        {
            Console.WriteLine($"{multiTargeted.Count} project(s) target several frameworks ({List(multiTargeted)}).");
            Console.WriteLine(MultiTargetedHint);
        }

        modernize.Notes.ForEach(Console.WriteLine);
        if (!write)
        {
            Console.WriteLine("== .editorconfig (run with --write to add it)");
            Console.Write(block);
            Console.Write(modernize.Block);
            return 0;
        }

        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var text = existing ?? "root = true\n";
        if (existing?.Contains(Migration.BeginMarker) == true && !existing.Contains("stylebro-migrate init"))
        {
            Console.WriteLine(".editorconfig already has settings from 'stylebro-migrate --write'; they include the built-in rules.");
        }
        else
        {
            text = Migration.Apply(text, block);
            Console.WriteLine("Wrote the built-in rule severities to .editorconfig.");
        }

        if (modernize.Block is not null)
        {
            text = Migration.Apply(text, modernize.Block, ModernizeBegin, ModernizeEnd);
            Console.WriteLine("Wrote the modernization rules to .editorconfig (see docs/modernizing.md).");
        }

        if (text != existing)
        {
            File.WriteAllText(path, text);
            Console.WriteLine("To report them on build too: <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in Directory.Build.props.");
        }

        return 0;
    }

    /// <summary>The block written into .editorconfig, between the stylebro-migrate markers.</summary>
    public static string Block()
    {
        var template = Template().Replace("\r\n", "\n").TrimEnd('\n');
        return Migration.BeginMarker + " (stylebro-migrate init: built-in .NET rules for StyleBro's preset; edits inside are replaced)\n"
            + template + "\n" + Migration.EndMarker + "\n";
    }

    /// <summary>
    /// The --modernize block: the SDK's rules that rewrite code into newer C# (tier B) and newer APIs (tier C), at warning
    /// where that's safe, else at suggestion with a note why. In a multi-targeted project the rules fire per target
    /// framework and 'dotnet format' writes the newer framework's edit into the shared file: the API rules break the older
    /// framework's build (CA1847 silently binds to LINQ there), the language rules too unless LangVersion is set (else each
    /// framework gets its own C# version). Keys the root .editorconfig sets itself are left out.
    /// </summary>
    public static (string? Block, List<string> Notes) Modernize(string root, IReadOnlyList<string> multiTargeted)
    {
        var withoutLangVersion = multiTargeted.Where(p => !SetsLangVersion(root, p)).ToList();
        var notes = new List<string>();
        if (withoutLangVersion.Count > 0)
        {
            notes.Add($"Modernize: the newer-C# rules (tier B) are suggestions: {withoutLangVersion.Count} multi-targeted project(s) don't set LangVersion ({List(withoutLangVersion)}).\n"
                + "  Each target framework then gets its own C# version (7.3 for .NET Framework and netstandard2.0); fixes made for the newer one break the older one.\n"
                + "  To turn them on: set <LangVersion>latest</LangVersion> (or a fixed version) in those projects or Directory.Build.props, then run 'stylebro-migrate init --modernize --write' again.");
        }

        if (multiTargeted.Count > 0)
        {
            notes.Add("Modernize: the newer-API rules (tier C) are suggestions: the repository has multi-targeted projects, the rules check the API per target framework,\n"
                + "  and 'dotnet format' would write e.g. ArgumentNullException.ThrowIfNull into code an older framework compiles too (CA1847's Contains(char) even binds to LINQ there).\n"
                + "  To use them in single-target projects, set them to warning in an .editorconfig in those projects' folders.");
        }

        var own = Migration.OwnKeys(root);
        var lines = ModernizeTemplate().Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
            .Select(l => l.Replace("{lang}", withoutLangVersion.Count == 0 ? "warning" : "suggestion").Replace("{api}", multiTargeted.Count == 0 ? "warning" : "suggestion"))
            .Where(l => l.StartsWith('#') || !l.Contains('=') || !own.Contains(l.Substring(0, l.IndexOf('=')).Trim()));
        var block = ModernizeBegin + " (stylebro-migrate init --modernize: built-in .NET modernization rules; edits inside are replaced)\n"
            + string.Join("\n", lines) + "\n" + ModernizeEnd + "\n";
        return (block, notes);
    }

    /// <summary>The template (sdk-rules.editorconfig, embedded).</summary>
    public static string Template() => Read("SdkRules.editorconfig");

    /// <summary>The modernization template (modernize-rules.editorconfig, embedded), with {lang}/{api} severities.</summary>
    public static string ModernizeTemplate() => Read("ModernizeRules.editorconfig");

    /// <summary>Project files under the root that set several target frameworks (relative paths).</summary>
    public static IEnumerable<string> MultiTargetedProjects(string root)
    {
        var several = new Regex(@"<TargetFrameworks>[^<]*;[^<]*</TargetFrameworks>", RegexOptions.IgnoreCase);
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || f.EndsWith(".props", StringComparison.OrdinalIgnoreCase)))
        {
            if (several.IsMatch(File.ReadAllText(file)))
            {
                yield return Path.GetRelativePath(root, file);
            }
        }
    }

    /// <summary>
    /// Whether a project (or .props) file under the root sets LangVersion, itself or in the Directory.Build.props it
    /// imports (the nearest one above it, and further up while each mentions its parent's).
    /// </summary>
    public static bool SetsLangVersion(string root, string relativePath)
    {
        // ponytail: text match, ignores conditions and custom imports; a wrong "no" only means suggestions instead of warnings.
        var langVersion = new Regex(@"<LangVersion>\s*[^<\s]", RegexOptions.IgnoreCase);
        string? file = Path.Combine(root, relativePath);
        var directory = Path.GetDirectoryName(file);
        while (file is not null)
        {
            var text = File.ReadAllText(file);
            if (langVersion.IsMatch(text))
            {
                return true;
            }

            if (Path.GetFileName(file).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
            {
                if (!text.Contains("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                directory = Path.GetDirectoryName(directory);
            }

            file = null;
            for (; directory is not null && directory.Length >= root.TrimEnd('\\', '/').Length; directory = Path.GetDirectoryName(directory))
            {
                var candidate = Path.Combine(directory, "Directory.Build.props");
                if (File.Exists(candidate))
                {
                    file = candidate;
                    break;
                }
            }
        }

        return false;
    }

    private static string List(IReadOnlyList<string> items) => string.Join(", ", items.Take(3)) + (items.Count > 3 ? ", ..." : string.Empty);

    private static string Read(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
