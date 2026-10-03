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
    /// What multi-targeted repositories need to know: plain 'dotnet format' crashes on IDE0055's fix there (Roslyn's
    /// linked-file merge, measured on Serilog and Newtonsoft.Json without StyleBro); 'stylebro-migrate format' doesn't.
    /// </summary>
    public const string MultiTargetedHint =
        "Run 'stylebro-migrate format' instead of 'dotnet format' here: with IDE0055 on, 'dotnet format' crashes on multi-targeted\n"
        + "projects (Roslyn can't merge the target frameworks' copies of a file). The command runs it once per target framework.";

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
        var path = Path.Combine(root, ".editorconfig");
        if (multiTargeted.Count > 0)
        {
            Console.WriteLine($"{multiTargeted.Count} project(s) target several frameworks ({string.Join(", ", multiTargeted.Take(3))}{(multiTargeted.Count > 3 ? ", ..." : string.Empty)}).");
            Console.WriteLine(MultiTargetedHint);
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
    public static string Block()
    {
        var template = Template().Replace("\r\n", "\n").TrimEnd('\n');
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
