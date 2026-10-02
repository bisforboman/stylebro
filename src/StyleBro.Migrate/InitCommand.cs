using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate init [path] [--write]: puts the severities of the built-in .NET rules the preset relies on into the
/// repository's root .editorconfig. They can't live in the preset: 'dotnet format' ignores rule severities in a package's
/// global config (the build doesn't), so only .editorconfig makes it fix them. Written between the same markers as
/// 'stylebro-migrate --write', which replaces the block with settings matched to a StyleCop setup.
/// </summary>
internal static class InitCommand
{
    public static int Run(string[] args)
    {
        var write = args.Contains("--write");
        var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        var block = Block();
        var path = Path.Combine(root, ".editorconfig");
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
    public static string Block() =>
        Migration.BeginMarker + " (stylebro-migrate init: built-in .NET rules for StyleBro's preset; edits inside are replaced)\n"
        + Template().Replace("\r\n", "\n").TrimEnd('\n') + "\n"
        + Migration.EndMarker + "\n";

    /// <summary>The template (sdk-rules.editorconfig, embedded).</summary>
    public static string Template()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SdkRules.editorconfig")
            ?? throw new InvalidOperationException("SdkRules.editorconfig is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
