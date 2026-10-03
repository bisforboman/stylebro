using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate init [path] [--write]: puts the severities of the built-in .NET rules the preset relies on into the
/// repository's root .editorconfig. They can't live in the preset: 'dotnet format' ignores rule severities in a package's
/// global config (the build doesn't), so only .editorconfig makes it fix them. Written between the same markers as
/// 'stylebro-migrate --write', which replaces the block with settings matched to a StyleCop setup.
/// </summary>
internal static class InitCommand
{
    /// <summary>
    /// Built-in rules whose 'dotnet format' fixes break multi-targeted projects (measured 2026-10-03 on Serilog and
    /// Newtonsoft.Json, without StyleBro): IDE0011 (now off: BRO1514-BRO1516) and IDE0055 (at warning, its fix runs in the style pass) crashed
    /// 'dotnet format' in Roslyn's linked-file merge (nothing written); IDE0040 (now off: BRO1404/BRO1007), IDE0047 (now off: BRO1405), IDE0048 (now off: BRO1406/BRO1407) and, together with
    /// StyleBro's fixes, the blank-line rules IDE2000/IDE2002/IDE2003 wrote conflict markers. Their fixes edit each target framework's copy of a file separately, and 'dotnet format' can't merge
    /// copies that came out different. StyleBro's own fixes give every copy the same text.
    /// </summary>
    public static readonly string[] UnsafeWhenMultiTargeted = { "IDE0055", "IDE2000", "IDE2002", "IDE2003" };

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
        var block = Block(multiTargeted.Count > 0);
        var path = Path.Combine(root, ".editorconfig");
        if (multiTargeted.Count > 0)
        {
            Console.WriteLine($"{multiTargeted.Count} project(s) target several frameworks ({string.Join(", ", multiTargeted.Take(3))}{(multiTargeted.Count > 3 ? ", ..." : string.Empty)}).");
            Console.WriteLine($"{string.Join(", ", UnsafeWhenMultiTargeted)} are written as suggestions: their 'dotnet format' fixes break multi-targeted");
            Console.WriteLine("projects (a crash or merge conflict markers). The IDE still shows them.");
        }

        if (!write)
        {
            Console.WriteLine("== .editorconfig (run with --write to add it)");
            Console.Write(block);
            return 0;
        }

        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        if (existing?.Contains(Migration.BeginMarker) == true && !existing.Contains("stylebro-migrate init"))
        {
            Console.Error.WriteLine(".editorconfig already has settings from 'stylebro-migrate --write'; they include these rules. Nothing to do.");
            return 0;
        }

        File.WriteAllText(path, Migration.Apply(existing ?? "root = true\n", block));
        Console.WriteLine("Wrote the built-in rule severities to .editorconfig.");
        Console.WriteLine("To report them on build too: <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in Directory.Build.props.");
        return 0;
    }

    /// <summary>The block written into .editorconfig, between the stylebro-migrate markers.</summary>
    public static string Block(bool multiTargeted = false)
    {
        var template = Template().Replace("\r\n", "\n").TrimEnd('\n');
        if (multiTargeted)
        {
            foreach (var id in UnsafeWhenMultiTargeted)
            {
                template = template.Replace(
                    $"dotnet_diagnostic.{id}.severity = warning",
                    $"# {id}: a suggestion here, its 'dotnet format' fix breaks multi-targeted projects (crash or conflict markers).\n"
                    + $"dotnet_diagnostic.{id}.severity = suggestion");
            }
        }

        return Migration.BeginMarker + " (stylebro-migrate init: built-in .NET rules for StyleBro's preset; edits inside are replaced)\n"
            + template + "\n" + Migration.EndMarker + "\n";
    }

    /// <summary>The template (sdk-rules.editorconfig, embedded).</summary>
    public static string Template()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SdkRules.editorconfig")
            ?? throw new InvalidOperationException("SdkRules.editorconfig is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

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
}
