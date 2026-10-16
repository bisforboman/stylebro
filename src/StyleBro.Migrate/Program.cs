using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate [path] [--write]: reads the StyleCop setup of the repository at 'path' (default: the current
/// directory) and prints the matching StyleBro and SDK settings. With --write it puts them into the repository's
/// .editorconfig files, between markers so a second run replaces them, and carries StyleCop suppressions in the code
/// ('#pragma warning disable SA1202', [SuppressMessage], &lt;NoWarn&gt;) over to the rules that replace them.
/// 'stylebro-migrate baseline' writes a baseline instead (<see cref="BaselineCommand"/>), 'stylebro-migrate init' the
/// built-in rule severities the preset relies on (<see cref="InitCommand"/>), 'stylebro-migrate format' runs 'dotnet
/// format' safely in multi-targeted repositories (<see cref="FormatCommand"/>).
/// </summary>
internal static class Program
{
    /// <summary>What 'stylebro-migrate --help' prints.</summary>
    public const string Usage = """
        stylebro-migrate: move a repository to StyleBro (https://bisforboman.github.io/stylebro/).
          Getting started: https://bisforboman.github.io/stylebro/getting-started/
          From StyleCop:   https://bisforboman.github.io/stylebro/migrating/
          Settings:        https://bisforboman.github.io/stylebro/configuration/

        Usage:
          stylebro-migrate [path] [--write] [--agents-md] [--sonar-profile <file>]
              Coming from StyleCop: reads the StyleCop setup at 'path' (default: the current folder) and prints the
              StyleBro and .NET settings that enforce the same things. --write puts them into the .editorconfig files,
              turns StyleBro's preset off and carries StyleCop suppressions over.

          stylebro-migrate init [path] [--write] [--modernize] [--agents-md] [--sonar-profile <file>] [--project <solution or project>]
              Without StyleCop: the built-in .NET rules StyleBro's preset relies on, for the root .editorconfig.
              --modernize adds the SDK's rules for newer C# and APIs. A repository with a StyleCop setup is told to
              use 'stylebro-migrate --write' instead. --project names what 'format' will run on (relative to 'path').

              --agents-md (both): also write a StyleBro section into AGENTS.md (instructions for AI agents, between
              markers, updated on every run). Opt-in.

              Both follow a SonarQube setup too (SonarAnalyzer.CSharp, Sonar rule severities in rulesets and configs):
              the Sonar rules that are on turn on the StyleBro and .NET rules that fix what they report. --sonar-profile
              takes a quality profile exported from the server (api/qualityprofiles/backup) instead of the defaults.

          stylebro-migrate format [folder, solution or project] [--files <a.cs> <b.cs> ...] [--all] [--once] [dotnet format options]
              'dotnet format' that fixes StyleBro's rules and the built-in rules init/migrate turn on (plus whitespace),
              once per target framework in multi-targeted repositories, never inside git submodules. Runs again until a
              run changes nothing (at most 3 runs) and prints the files each run changed (the first 10); --once runs once. --all also
              applies every other analyzer's and compiler fix. Other options pass through (--severity warn, --report). A
              folder with several solutions or projects needs one named.
              Then it lists the findings StyleBro's fixes keep on purpose, with the reason (a name read by reflection,
              used in generated code, ...): don't change those by hand without checking; suppress or baseline them.
              --verify-no-changes changes nothing and checks whether formatting would change a file; kept findings
              don't fail it (plain 'dotnet format --verify-no-changes' fails on them). For CI and agents.
              Exit codes: 0 clean (only kept findings left, if any); 2 a run still changed files after 3 runs, or with
              --verify-no-changes, formatting would change a file; other: 'dotnet format' failed.
              --files formats only those files (paths relative to the current folder or the repository root), loading
              just the project each belongs to: seconds after a small edit. With --verify-no-changes too.

          stylebro-migrate [path] --diff[=<file>] [--keep] [--all] [--no-restore] [--project <solution or project>] [--sonar-profile <file>]
          stylebro-migrate init [path] --diff[=<file>] [--keep] [--all] [--no-restore] [--project <solution or project>] [--modernize] [--sonar-profile <file>]
          stylebro-migrate format [folder, solution or project] --diff[=<file>] [--keep] [--project <solution or project>] [options]
              Preview: runs the command (--write for the first two) and then format until a run changes nothing on a
              temporary copy of the repository, which is never touched. Prints a summary (settings, files changed per
              rule, sample changes) and writes the full diff to .git/stylebro-preview.patch (or <file>). Adds the
              StyleBro.Analyzers reference in the copy when the repository has none. --keep keeps the copy; --project
              names what format runs on when the folder has several solutions or projects; --no-restore uses the
              repository's own restore (run 'dotnet restore' first).

          stylebro-migrate baseline [path or solution or project] [--project <solution or project>]
              Writes stylebro.baseline with today's violations, so only new code has to follow the rules.

          --json (any command): one JSON object on stdout (settings, conventions, runs, changes per rule, kept
              findings, clean, exit code; https://bisforboman.github.io/stylebro/agents/); the text goes to stderr.

          stylebro-migrate help | --help | -h
              This text.

          stylebro-migrate --version
              The tool's version.
        """;

    /// <summary>The option that names a SonarQube quality profile backup (XML) to follow.</summary>
    internal const string SonarProfileOption = "--sonar-profile";

    /// <summary>How to format after init or --write: 'stylebro-migrate format', not plain 'dotnet format'.</summary>
    internal const string FormatHint = "Next: run 'stylebro-migrate format'. Plain 'dotnet format' also applies every other analyzer's and the compiler's fixes.";

    /// <summary>The next step after init when no project references StyleBro.Analyzers yet.</summary>
    internal const string AddPackageHint = "Next: add the StyleBro.Analyzers package (https://bisforboman.github.io/stylebro/getting-started/), then run 'stylebro-migrate format'.";

    /// <summary>What to do after --write: swap the packages and format.</summary>
    internal const string NextStep = "Next: add the StyleBro.Analyzers package, remove StyleCop.Analyzers, and run 'stylebro-migrate format'.";

    /// <summary>What to do after --write when StyleBro.Analyzers is already in and StyleCop.Analyzers still is.</summary>
    internal const string RemoveStyleCopStep = "Next: remove StyleCop.Analyzers, and run 'stylebro-migrate format'.";

    /// <summary>Gets the tool's version ('0.6.0-alpha.1', without the commit the SDK appends).</summary>
    internal static string Version =>
        (typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown").Split('+')[0];

    public static int Main(string[] args)
    {
        if (args.FirstOrDefault() == "help" || args.Any(a => a is "--help" or "-h" or "-?"))
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (args is ["--version"])
        {
            Console.WriteLine(Version);
            return 0;
        }

        if (!args.Contains(JsonReport.Option))
        {
            return Guarded(args);
        }

        // --json: the human text goes to stderr, the report to stdout.
        args = args.Where(a => a != JsonReport.Option).ToArray();
        var command = args.FirstOrDefault() is "baseline" or "init" or "format" ? args[0] : "migrate";
        var preview = command != "baseline" && PreviewCommand.Wants(args);
        JsonReport.Begin(preview ? "preview" : command, preview ? command : null);
        var stdout = Console.Out;
        Console.SetOut(Console.Error);
        int code;
        try
        {
            code = Guarded(args);
        }
        finally
        {
            Console.SetOut(stdout);
        }

        stdout.WriteLine(JsonReport.End(code));
        return code;
    }

    /// <summary>stylebro-migrate [path] [--write]: the migration from StyleCop.</summary>
    public static int Migrate(string[] args)
    {
        var (profile, rest) = TakeOption(args, SonarProfileOption);
        var write = rest.Contains("--write");
        var root = Path.GetFullPath(rest.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        string? noWorkspace = null;
        root = RepositoryFolder(root, ref noWorkspace);
        JsonReport.Set("path", root);
        JsonReport.Set("write", write);
        if (!ReadSonar(root, profile, out var sonar))
        {
            return 1;
        }

        var setup = StyleCopSetup.Read(root);

        // StyleCop settings no project uses are leftovers (Spectre.Console): init's job, also with a Sonar setup, which init follows
        // too (Splat: a leftover stylecop.json read with StyleCop's defaults next to Sonar renamed 72 files' '_fields').
        if (setup.NotInstalled)
        {
            setup.Warnings.ToList().ForEach(w => Console.WriteLine("Warning: " + w));
            var evidence = StyleCopSetup.Evidence(root);
            Console.WriteLine(LeftoverLine(evidence));

            // Without settings there's nothing to call leftovers (trial 23, tools/Gen: "no StyleCop settings" then "StyleCop's
            // severities there don't describe the code").
            Console.WriteLine(evidence.Count == 0
                ? $"Nothing to migrate{(write ? ", nothing written" : string.Empty)}. Run 'stylebro-migrate init' instead: it follows the code's own conventions{(sonar is null ? string.Empty : " and the Sonar setup")}."
                : $"Nothing to migrate{(write ? ", nothing written" : string.Empty)}: StyleCop's severities there don't describe the code (every rule they don't mention would count as on). Run 'stylebro-migrate init' instead: it follows the code's own conventions{(sonar is null ? string.Empty : " and the Sonar setup")} and ignores those lines.");
            return 1;
        }

        var result = Migration.Generate(setup, root, sonar: sonar);
        var plan = Migration.Plan(setup, root, result);
        var optionScopes = new List<(string Section, List<string> Lines)>();
        if (sonar is not null)
        {
            optionScopes = Suppressions.OptionScopes(root, sonar, result.Lines);
            plan[".editorconfig"].AddRange(optionScopes);
        }

        Console.WriteLine($"Read: {(setup.Sources.Count == 0 ? "no StyleCop settings (StyleCop's defaults)" : string.Join(", ", setup.Sources))}");
        if (setup.Version is { } version)
        {
            Console.WriteLine($"StyleCop.Analyzers version: {version}{(setup.VersionRecorded ? " (recorded by the last --write; the package is gone)" : string.Empty)}{(version.StartsWith("1.2", StringComparison.Ordinal) ? string.Empty : " (rules added in later versions count as off)")}");
        }
        else if (setup.UnresolvedVersion is { } unresolved)
        {
            Console.WriteLine($"StyleCop.Analyzers version: unknown (the reference says {unresolved}, and no MSBuild file in the repository defines it): every StyleCop 1.2 rule counts.");
        }

        if (setup.StyleSharp is { } styleSharp)
        {
            Console.WriteLine($"StyleSharp.Analyzers{(styleSharp.Version is { } v ? " " + v : string.Empty)}: its SST rules count as the StyleCop rules they stand for (SST1309 as StyleCop's SX1309: fields start with '_'), its stylesharp.* settings as stylecop.json's.");
        }

        foreach (var line in ScopeReport(setup))
        {
            Console.WriteLine(line);
        }

        Report(setup, result);
        if (result.Sonar is { } applied)
        {
            Suppressions.Annotate(sonar!.Report(applied), optionScopes).ForEach(Console.WriteLine);
        }

        foreach (var note in result.Notes)
        {
            Console.WriteLine(note);
        }

        if (Migration.MigrationFolders(root) is { Count: > 0 } migrations)
        {
            Console.WriteLine($"EF Core migrations in {string.Join(", ", migrations)}: marked generated_code = true, so formatting and StyleBro leave them alone.");
        }

        if (Migration.VendoredFolders(root) is { Count: > 0 } vendored)
        {
            Console.WriteLine($"Vendored code in {string.Join(", ", vendored)}: marked generated_code = true, so formatting and StyleBro leave it alone.");
        }

        var suppressions = RewriteSuppressions(root, Suppressions.WithoutOff(Suppressions.WithSonar(Suppressions.WithStyleSharp(result.Replacements, setup), sonar), plan.Values.SelectMany(blocks => blocks).SelectMany(b => b.Lines)), write, sonar is null ? setup.Folders : null);
        Console.WriteLine(suppressions.Added == 0
            ? suppressions.Already > 0 ? $"Suppressions: already carried over ({suppressions.Already} files suppress the replacing rules too)." : "Suppressions: none in the code to carry over."
            : $"Suppressions: {suppressions.Added} {(write ? "added" : "to add")} for the replacing rules in {suppressions.Files} files (the original ones stay).");

        // SSH.NET kept its stylecop.json without a word: StyleBro never reads it, the settings are in the block now.
        if (setup.Sources.Where(s => Path.GetFileName(s).Equals("stylecop.json", StringComparison.OrdinalIgnoreCase)).ToList() is { Count: > 0 } styleCopJson)
        {
            // Trial 25: after the swap the report still said "once StyleCop.Analyzers is gone, remove it", and the stylecop.json
            // settings were lost when it was; the block records them now, so the file can go.
            var it = styleCopJson.Count == 1 ? "it" : "them";
            Console.WriteLine($"{string.Join(", ", styleCopJson.Select(s => s.Replace('\\', '/')))}: StyleBro doesn't read {it}; the settings are in the block now, which also records them for later runs. {(setup.VersionRecorded ? "StyleCop.Analyzers is gone, so" : "Once StyleCop.Analyzers is gone,")} {it} can go, with the <AdditionalFiles> item{(styleCopJson.Count == 1 ? string.Empty : "s")} naming {it}.");
        }

        if (suppressions.NotCarried > 0)
        {
            Console.WriteLine($"Suppressions not carried over: {suppressions.NotCarried} of whitespace rules (like SA1028) that IDE0055 covers, because 'dotnet format' formats whitespace whatever a suppression says. Usually nothing to do: format changes such a line only where its whitespace breaks your formatting settings. To keep that code exactly as it is, exclude its files from formatting (generated_code = true in an .editorconfig section for them).");
        }

        Console.WriteLine();
        foreach (var (file, sections) in plan)
        {
            var path = Path.Combine(root, file);
            var block = Migration.Render(sections, setup.StyleSharp is { WithStyleCop: false } ? "StyleSharp" : "StyleCop");
            JsonReport.AddSettings(file, block);
            if (write)
            {
                PreviewCommand.EditFile(path, existing => Migration.Apply(existing, block));
                Console.WriteLine($"Wrote the settings to {file}.");
            }
            else
            {
                Console.WriteLine($"== {file}");
                Console.Write(block);
                Console.WriteLine();
            }
        }

        // The whitespace keys a nearer .editorconfig decides: init's block there (null: a stale one goes).
        foreach (var (source, lines) in result.Whitespace)
        {
            var file = source + "/.editorconfig";
            var path = Path.Combine(root, source, ".editorconfig");
            if (lines is null)
            {
                if (write)
                {
                    PreviewCommand.EditFile(path, existing => InitCommand.RemoveWhitespaceBlock(existing ?? string.Empty));
                }

                Console.WriteLine(write ? $"Removed the stylebro-folder block from {file}: nothing needs it any more." : $"== {file}: its stylebro-folder block is no longer needed (--write removes it)");
                continue;
            }

            var block = InitCommand.WhitespaceBlock(lines);
            JsonReport.AddSettings(file, block);
            if (write)
            {
                PreviewCommand.EditFile(path, existing => Migration.Apply(InitCommand.RemoveWhitespaceBlock(existing ?? string.Empty, legacyOnly: true), block, InitCommand.WhitespaceBegin, InitCommand.WhitespaceEnd));
                Console.WriteLine($"Wrote the whitespace settings it decides to {file}.");
            }
            else
            {
                Console.WriteLine($"== {file}");
                Console.Write(block);
                Console.WriteLine();
            }
        }

        // Every Directory.Build.props a project imports where StyleCop runs (a nested one shadows the root's): elsewhere no
        // settings are written, so the preset stays (vs-validation's tests).
        foreach (var props in Migration.PropsFiles(root, setup.Folders))
        {
            var disabled = Migration.DisablePreset(File.Exists(props) ? File.ReadAllText(props) : null, importParent: !File.Exists(props) && Migration.HasParentProps(props));
            var name = Path.GetRelativePath(root, props).Replace('\\', '/');
            if (disabled is null)
            {
                continue;
            }

            var hidden = Migration.HiddenPropsNote(props);
            if (write)
            {
                PreviewCommand.EditFile(props, _ => disabled);
                Console.WriteLine($"Turned the preset off in {name} (<StyleBroPreset>none</StyleBroPreset>): the settings above replace it.");
            }
            else
            {
                Console.WriteLine($"--write also turns the preset off in {name} (<StyleBroPreset>none</StyleBroPreset>): the settings replace it.");
            }

            if (hidden is not null)
            {
                Console.WriteLine(hidden);
            }
        }

        AgentsFile.Update(root, write, rest.Contains(AgentsFile.Option)).ForEach(Console.WriteLine);

        // Where the swap goes first, then what to do next.
        PackageHints(setup, root).ForEach(Console.WriteLine);
        Console.WriteLine(write
            ? NextAfterWrite(setup, root)
            : "Run with --write to put these settings into the .editorconfig files and carry the suppressions over.");
        PrintWorkspaceHint(root);
        return 0;
    }

    /// <summary>
    /// The next step after --write, from where the package swap stands: both packages still to swap, StyleCop still to
    /// remove, only format left (a second --write after the swap said "add StyleBro.Analyzers" again), or StyleBro to add.
    /// </summary>
    internal static string NextAfterWrite(StyleCopSetup setup, string root) =>
        (PreviewCommand.ReferencesStyleBro(root), setup.ReferencedIn.Count > 0 || (setup.ReferencedOutside.Count > 0 && setup.RemovedIn.Count == 0)) switch
        {
            (false, true) => NextStep.Replace("StyleCop.Analyzers", setup.PackageName, StringComparison.Ordinal),
            (true, true) => RemoveStyleCopStep.Replace("StyleCop.Analyzers", setup.PackageName, StringComparison.Ordinal),
            (true, false) => FormatHint,
            _ => AddPackageHint,
        };

    /// <summary>What the migration and init say about StyleCop settings no project uses (<see cref="StyleCopSetup.NotInstalled"/>).</summary>
    internal static string LeftoverLine(IReadOnlyCollection<string> evidence) => evidence.Count == 0
        ? "StyleCop.Analyzers (or StyleSharp.Analyzers) isn't referenced by any project, and there are no StyleCop settings: StyleCop doesn't run here."
        : $"StyleCop.Analyzers (or StyleSharp.Analyzers) isn't referenced by any project, so StyleCop doesn't run here: its settings are leftovers ({string.Join(", ", evidence)}).";

    /// <summary>
    /// The folder init and the migration work on (trial 22): the given one, or the git repository's root when the build reaches
    /// past the folder: a Directory.Build.props (one the folder's own doesn't shadow), Directory.Packages.props, .editorconfig
    /// (one the folder's 'root = true' doesn't stop) or .globalconfig above it, or a solution in it that lists projects outside
    /// it. From src/, init wrote a src/Directory.Build.props that shadowed the root's (every project lost StyleBro,
    /// TreatWarningsAsErrors and Nullable); from build/, where only the solution was, it wrote settings no project read.
    /// <paramref name="workspace"/> (relative to the folder) is made relative to the result.
    /// </summary>
    internal static string RepositoryFolder(string folder, ref string? workspace)
    {
        folder = Path.GetFullPath(folder).TrimEnd('\\', '/');
        if (FormatCommand.RepositoryRoot(folder) is not { } top || string.Equals(top.TrimEnd('\\', '/'), folder, StringComparison.OrdinalIgnoreCase))
        {
            return folder;
        }

        top = top.TrimEnd('\\', '/');
        var ownProps = Path.Combine(folder, "Directory.Build.props");
        var propsShadowed = File.Exists(ownProps) && !Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(ownProps)), @"<Import\b[^>]*Directory\.Build\.props", RegexOptions.IgnoreCase);
        var editorConfigStops = StyleCopSetup.IsEditorConfigRoot(folder);
        var reasons = new List<string>();
        for (var above = Path.GetDirectoryName(folder); above is not null && above.Length >= top.Length; above = Path.GetDirectoryName(above))
        {
            reasons.AddRange(new[] { "Directory.Build.props", "Directory.Packages.props", ".editorconfig", ".globalconfig" }
                .Where(name => File.Exists(Path.Combine(above, name)) && !(name == "Directory.Build.props" && propsShadowed) && !(name == ".editorconfig" && editorConfigStops))
                .Select(name => Path.GetRelativePath(top, Path.Combine(above, name)).Replace('\\', '/')));
        }

        reasons.AddRange(Directory.EnumerateFiles(folder)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".sln" or ".slnx" or ".slnf" && Regex.IsMatch(File.ReadAllText(f), @"""\.\.[\\/]"))
            .Select(f => $"{Path.GetFileName(f)} (lists projects outside it)"));
        if (reasons.Count == 0)
        {
            return folder;
        }

        if (workspace is { Length: > 0 })
        {
            workspace = Path.GetRelativePath(top, Path.Combine(folder, workspace));
        }

        Console.WriteLine($"Working on the repository's root {top}: the build of {Path.GetRelativePath(top, folder).Replace('\\', '/')}/ reaches past it ({string.Join(", ", reasons)}).");
        return top;
    }

    /// <summary>The next step after init: format, or first add the package when no MSBuild file references it.</summary>
    internal static string NextHint(string root) => PreviewCommand.ReferencesStyleBro(root) ? FormatHint : AddPackageHint;

    /// <summary>Where StyleCop runs, when only some projects reference it: the settings apply only there.</summary>
    internal static List<string> ScopeReport(StyleCopSetup setup)
    {
        static string Names(IReadOnlyList<string> folders) =>
            string.Join(", ", folders.Take(5)) + (folders.Count > 5 ? $" and {folders.Count - 5} more" : string.Empty);

        // Named after the package that runs, and by the shorter list (trial 24: Refit's StyleSharp ran everywhere but one
        // folder, and the report said "StyleCop runs only in" 19 of them).
        var tool = setup.StyleSharp is { WithStyleCop: false } ? "StyleSharp" : "StyleCop";
        var lines = setup.Folders is { } folders
            ? new List<string>
            {
                folders.Count > setup.FoldersWithout.Count
                    ? $"{tool} runs everywhere except {Names(setup.FoldersWithout)}: the settings apply where it runs, with the rules {tool} has on there."
                    : $"{tool} runs only in {Names(folders)}: the settings apply there, with the rules {tool} has on there.",
                $"No {tool} in {Names(setup.FoldersWithout)}: StyleBro's rules and the built-in ones the block sets are off there by id, so the build and 'stylebro-migrate format' leave them alone. To have them checked and formatted too, remove the block's '# No StyleCop ran in' line and its section for them.",
            }
            : new List<string>();
        lines.AddRange(setup.Warnings.Select(w => "Warning: " + w));
        return lines;
    }

    /// <summary>Where the package swap goes: the files that reference StyleCop.Analyzers, and the ones that remove it.</summary>
    internal static List<string> PackageHints(StyleCopSetup setup, string root)
    {
        var lines = new List<string>();
        if (setup.ReferencedIn.Count > 0)
        {
            // How the reference looks: only the cases this repository has (a GlobalPackageReference, central package management).
            var files = setup.ReferencedIn.Select(f => Path.Combine(root, f)).Where(File.Exists).ToList();
            var global = files.Any(f => Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(f)), @"<GlobalPackageReference\b[^>]*?\bInclude\s*=\s*""StyleCop\.Analyzers", RegexOptions.IgnoreCase));
            var central = files.Select(f => PreviewCommand.CentralPackages(f, root)).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).ToList();
            var how = new List<string>();
            if (global)
            {
                how.Add("as a GlobalPackageReference, like StyleCop's");
            }

            if (central.Count > 0)
            {
                how.Add($"{(global ? "otherwise " : string.Empty)}its version in {string.Join(", ", central)} (central package management)");
            }

            lines.Add($"{setup.PackageName} {(setup.StyleSharp is { WithStyleCop: true } ? "are" : "is")} referenced in {string.Join(", ", setup.ReferencedIn)}: StyleBro.Analyzers goes there{(how.Count == 0 ? string.Empty : ": " + string.Join("; ", how))}.");
        }

        // A submodule's file can't be edited here (SixLabors' shared-infrastructure): the repository's own props swap it.
        if (setup.ReferencedOutside.Count > 0 && setup.RemovedIn.Count == 0)
        {
            var props = string.Join(", ", Migration.PropsFiles(root).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')));
            lines.Add($"StyleCop.Analyzers is referenced in {string.Join(", ", setup.ReferencedOutside)}, outside this repository's own files (a git submodule), which can't be edited here: swap it in {props}, after the line that imports that file: <PackageReference Remove=\"StyleCop.Analyzers\" /> and StyleBro.Analyzers' <PackageReference Include=\"StyleBro.Analyzers\" Version=\"...\" PrivateAssets=\"all\" /> (https://bisforboman.github.io/stylebro/migrating/#stylecop-referenced-in-a-git-submodule).");
        }

        // Old-style projects install packages through packages.config: a PackageReference would switch their restore.
        if (PackagesConfig.Projects(StyleCopSetup.EnumerateFiles(root)) is { Count: > 0 } packagesConfig)
        {
            var names = packagesConfig.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToList();
            lines.Add($"{string.Join(", ", names.Take(3))}{(names.Count > 3 ? $" and {names.Count - 3} more" : string.Empty)} use packages.config: install StyleBro.Analyzers there the packages.config way (Visual Studio's package manager writes the packages.config entry, the <Analyzer> items and the build targets import), and give a PackageReference in Directory.Build.props Condition=\"{PackagesConfig.NotPackagesConfig}\" (https://bisforboman.github.io/stylebro/getting-started/#old-style-non-sdk-projects).");
        }

        // Not a file that swaps StyleCop for StyleBro itself (the submodule case above, done).
        var removedIn = setup.RemovedIn.Where(f => !File.Exists(Path.Combine(root, f)) || !Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(Path.Combine(root, f))), @"Include\s*=\s*""StyleBro\.Analyzers""", RegexOptions.IgnoreCase)).ToList();
        if (removedIn.Count > 0)
        {
            lines.Add($"{string.Join(", ", removedIn)} remove{(removedIn.Count == 1 ? "s" : string.Empty)} the StyleCop.Analyzers reference (<PackageReference Remove=\"StyleCop.Analyzers\" />): add <PackageReference Remove=\"StyleBro.Analyzers\" /> next to it, else that project gets StyleBro (and a restore error, NU1015, when it doesn't use central package management).");
        }

        return lines;
    }

    /// <summary>
    /// The hints for running format after init or the migration: when the folder has several solutions or projects, which
    /// ones (format needs one named), and the CI steps a plain 'dotnet format --verify-no-changes' would fail (<see cref="VerifyNoChangesSteps"/>).
    /// </summary>
    internal static void PrintWorkspaceHint(string root, string? named = null)
    {
        if (named is { Length: > 0 })
        {
            Console.WriteLine($"To format it: 'stylebro-migrate format {named}'.");
        }
        else if (BaselineCommand.WorkspaceCandidates(root) is { Count: > 1 } several)
        {
            Console.WriteLine($"This folder has several solutions or projects ({string.Join(", ", several)}): name the one to format, e.g. 'stylebro-migrate format {several[0]}'.");
        }

        VerifyNoChangesSteps(root).ForEach(Console.WriteLine);
    }

    /// <summary>
    /// The CI steps (GitHub workflows, Azure Pipelines) that run plain 'dotnet format --verify-no-changes', one note each
    /// ('file:line'): findings StyleBro keeps on purpose (a rename a guard holds back) fail them for good (sourcegit).
    /// </summary>
    internal static List<string> VerifyNoChangesSteps(string root)
    {
        var workflows = Path.Combine(root, ".github", "workflows");
        var files = (Directory.Exists(workflows) ? Directory.EnumerateFiles(workflows).Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)) : [])
            .Concat(Directory.EnumerateFiles(root, "azure-pipelines*.y*ml"))
            .Order(StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"\bdotnet\s+format\b.*--verify-no-changes") && !lines[i].TrimStart().StartsWith('#'))
                {
                    notes.Add($"{Path.GetRelativePath(root, file).Replace('\\', '/')}:{i + 1} runs 'dotnet format --verify-no-changes': findings StyleBro keeps on purpose (a rename that would break reflection, for example) fail it every time. Run 'stylebro-migrate format --verify-no-changes' there instead (it checks the same and accepts those), or write a baseline (stylebro-migrate baseline).");
                }
            }
        }

        return notes;
    }

    /// <summary>
    /// An option with a value ('--name value' or '--name=value'): the value (null when it's not there), and the other
    /// arguments.
    /// </summary>
    internal static (string? Value, string[] Others) TakeOption(string[] args, string name)
    {
        var rest = args.ToList();
        var at = rest.FindIndex(a => a == name || a.StartsWith(name + "=", StringComparison.Ordinal));
        if (at < 0)
        {
            return (null, args);
        }

        var value = rest[at].Length > name.Length ? rest[at].Substring(name.Length + 1) : at + 1 < rest.Count ? rest[at + 1] : string.Empty;
        rest.RemoveRange(at, rest[at].Length > name.Length || at + 1 >= rest.Count ? 1 : 2);
        return (value, rest.ToArray());
    }

    /// <summary>
    /// The SonarQube setup at the root (<paramref name="profile"/>: --sonar-profile's file), or null without one; false
    /// (with the error printed) when the profile isn't a readable file.
    /// </summary>
    internal static bool ReadSonar(string root, string? profile, out SonarSetup? sonar)
    {
        sonar = null;
        if (profile is not null && !File.Exists(profile))
        {
            Console.Error.WriteLine($"{SonarProfileOption}: not a file: {profile}");
            return false;
        }

        try
        {
            sonar = SonarSetup.Read(root, profile is null ? null : Path.GetFullPath(profile));
            return true;
        }
        catch (System.Xml.XmlException e)
        {
            Console.Error.WriteLine($"{SonarProfileOption}: {profile} isn't a quality profile backup ({e.Message}).");
            return false;
        }
    }

    /// <summary>
    /// A dropped rule's reason as the report shows it. The mapping is written for the project's own docs and carries survey
    /// evidence (finding counts in the surveyed repositories, which decision it was); users only need the reason.
    /// </summary>
    internal static string UserFacingReason(string proposal)
    {
        var reason = Regex.Replace(proposal, @"^(Drop|Not applicable)[:.]\s*", string.Empty);
        reason = Regex.Replace(reason, @"\s*\([^)]*\buser decision\)", string.Empty);
        var sentences = Regex.Split(reason, @"(?<=\.)\s+").Where(s => !Regex.IsMatch(s, @"\bfindings?\b|private app", RegexOptions.IgnoreCase));
        return string.Join(" ", sentences).Trim();
    }

    internal static Dictionary<string, string> LoadMapping()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StyleCopMapping.csv")
            ?? throw new InvalidOperationException("The StyleCop mapping isn't embedded.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var comma = line.IndexOf(',');
            var proposal = line.Substring(comma + 1).Trim('"').Replace("\"\"", "\"").Replace("**", string.Empty).Replace("`", string.Empty);
            mapping[line.Substring(0, comma)] = proposal;
        }

        return mapping;
    }

    /// <summary>
    /// Carries the suppressions in the code over (<see cref="Suppressions"/>): StyleCop's and Sonar's ids, to the rules that
    /// replace them. Returns how many were (or would be) added, in how many files, and how many weren't carried over (IDE0055).
    /// </summary>
    internal static (int Added, int Files, int NotCarried, int Already) RewriteSuppressions(string root, IReadOnlyDictionary<string, SortedSet<string>> replacements, bool write, IReadOnlyList<string>? folders = null)
    {
        int added = 0;
        int files = 0;
        int notCarried = 0;
        int already = 0;
        var mentions = new Regex(@"\bS(?:[AX]|ST)?\d{3,4}\b", RegexOptions.IgnoreCase);

        // Suppressions of a rule 'dotnet format' ignores can't take effect: its whitespace pass ignores
        // '#pragma warning disable IDE0055' (Spectre.Console's SA1028 pragmas got one), so none is added; the report says so.
        var all = replacements.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
        var effective = replacements.ToDictionary(r => r.Key, r => new SortedSet<string>(r.Value.Where(id => !Suppressions.Ineffective.Contains(id)), StringComparer.Ordinal), StringComparer.OrdinalIgnoreCase);
        var ids = effective.Values.SelectMany(v => v).Distinct().ToList();
        var replacing = new Regex(ids.Count == 0 ? "(?!)" : $@"\b(?:{string.Join("|", ids.Select(Regex.Escape))})\b");

        // Generated code isn't the team's: checked-in source generator output (Spectre.Console's Generated/ folders) stays as is.
        var marked = Migration.MarkedGeneratedFolders(root);
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || StyleCopSetup.IsMSBuild(f)))
        {
            var bytes = File.ReadAllBytes(file);
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            // Only where StyleCop ran (trial 22, kafkaflow: samples' 'NoWarn SA1600' got BRO1601 although nothing runs there): the
            // block turns no rule on elsewhere. An MSBuild file above such a folder reaches it.
            var folder = Path.GetDirectoryName(relative)!.Replace('\\', '/');
            if (folders is not null && !folders.Any(f => Migration.IsUnder(relative, f) || (StyleCopSetup.IsMSBuild(file) && Migration.IsUnder(f.TrimEnd('/') + "/", folder))))
            {
                continue;
            }

            if (!mentions.IsMatch(text)
                || (!StyleCopSetup.IsMSBuild(file) && (Migration.IsGeneratedOrVendored(relative, text) || marked.Any(m => relative.StartsWith(m.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))))
            {
                continue;
            }

            (string Text, int Added) Rewrite(IReadOnlyDictionary<string, SortedSet<string>> ids) => StyleCopSetup.IsMSBuild(file)
                ? Suppressions.RewriteNoWarn(text, ids)
                : Suppressions.Rewrite(text, ids);
            var (rewritten, count) = Rewrite(effective);
            notCarried += Rewrite(all).Added - count;
            if (count == 0)
            {
                // A second run: the replacing ids are there already (the report said "none to carry over", trial 24).
                already += replacing.IsMatch(text) ? 1 : 0;
                continue;
            }

            added += count;
            files++;
            if (write)
            {
                PreviewCommand.EditFile(file, _ => rewritten);
            }
        }

        return (added, files, notCarried, already);
    }

    /// <summary>The StyleSharp rules that are on and stand for no StyleCop rule (<see cref="StyleCopSetup.StandsFor"/>), or none without StyleSharp.</summary>
    internal static List<string> StyleSharpOnly(StyleCopSetup setup) => setup.StyleSharp is not { } styleSharp || setup.Raw is not { } raw
        ? []
        : StyleCopSetup.StyleSharpRules
            .Where(r => raw.TryGetValue(r.Id, out var s) && s >= Severity.Suggestion && StyleCopSetup.StandsFor(r.Id, r.StyleCop, styleSharp).Count == 0)
            .Select(r => r.Id)
            .ToList();

    /// <summary>Runs the command the arguments name.</summary>
    /// <summary>The command; a dotnet command it depends on that failed (MSBuild couldn't evaluate a project) ends it with that command's output, not a stack trace.</summary>
    private static int Guarded(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (ToolException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Dispatch(string[] args)
    {
        // A 'format' killed while it ran leaves '#' swapped in the files it protected: every command puts them back first.
        FormatCommand.DirectiveMask.Recover(Console.WriteLine);

        // Copies a killed preview or verify left behind (trial 23: hundreds in the temp folder).
        PreviewCommand.DeleteStaleCopies(Path.GetTempPath(), TimeSpan.FromDays(1));

        // 'format' passes unknown options on to 'dotnet format'.
        var (command, known) = args.FirstOrDefault() switch
        {
            "baseline" => ("baseline", new[] { "--project" }),
            "init" => ("init", new[] { "--write", "--modernize", AgentsFile.Option, SonarProfileOption }.Concat(PreviewCommand.Options).ToArray()),
            "format" => ("format", null),
            _ => (null, new[] { "--write", AgentsFile.Option, SonarProfileOption }.Concat(PreviewCommand.Options).ToArray()),
        };
        var options = args.Skip(command is null ? 0 : 1).ToArray();
        if (known is not null && options.FirstOrDefault(a => a.StartsWith('-') && !known.Contains(a.Split('=')[0])) is { } unknown)
        {
            Console.Error.WriteLine($"Unknown option: {unknown}");
            Console.Error.WriteLine(Usage);
            return 1;
        }

        if (command != "baseline" && PreviewCommand.Wants(options))
        {
            if (options.Contains("--write"))
            {
                Console.Error.WriteLine("--diff previews without writing anything: use --diff or --write, not both.");
                return 1;
            }

            // 'init --diff' on a StyleCop repository: init's refusal, before the copy (trial 23: it ended "The preview failed").
            // Not before the preview's own check of what format runs on (trial 24: 42 s of evaluations, then "several solutions").
            if (command == "init" && Path.GetFullPath(TakeOption(TakeOption(options, "--project").Others, SonarProfileOption).Others.FirstOrDefault(a => !a.StartsWith('-')) ?? ".") is var folder
                && Directory.Exists(folder) && (TakeOption(options, "--project").Value is not null || BaselineCommand.FindWorkspace(folder) is not null) && !InitCommand.IsMigrated(folder)
                && StyleCopSetup.Evidence(folder).Count > 0 && StyleCopSetup.IsInstalled(folder) && InitCommand.SendsToMigration(folder))
            {
                return 1;
            }

            return PreviewCommand.Run(command, options);
        }

        switch (command)
        {
            case "baseline":
                return BaselineCommand.Run(options);

            case "init":
                return InitCommand.Run(options);

            case "format":
                // With --json, 'dotnet format''s own output goes through the log too (else it inherits stdout).
                return FormatCommand.Run(options, JsonReport.Current is null ? null : Console.Error.WriteLine);

            default:
                return Migrate(options);
        }
    }

    /// <summary>Which StyleCop rules are on, and why the ones StyleBro and the SDK don't cover aren't.</summary>
    private static void Report(StyleCopSetup setup, Migration.Result result)
    {
        // SA0001 says that XML documentation isn't parsed: with a GenerateDocumentationFile project it can't fire, so it
        // isn't a rule the repository loses.
        var cannotFire = Migration.CannotFire(setup);
        var on = StyleCopSetup.Rules.Where(r => setup.IsOn(r.Id) && !(r.Id == "SA0001" && setup.DocumentationParsed) && !cannotFire.Contains(r.Id)).Select(r => r.Id).ToList();
        var uncovered = on.Where(id => !result.Covered.Contains(id)).ToList();
        Console.WriteLine($"StyleCop rules on{(setup.StyleSharp is null ? string.Empty : " (StyleSharp's, read as the StyleCop rules they stand for)")}: {on.Count}; enforced by StyleBro or the SDK after migrating: {on.Count - uncovered.Count}");

        // StyleSharp's rules StyleCop doesn't have (digit separators, complexity, ...): no StyleBro rule replaces them.
        if (StyleSharpOnly(setup) is { Count: > 0 } own)
        {
            Console.WriteLine($"StyleSharp rules on that StyleCop doesn't have ({own.Count}): StyleBro doesn't replace them, so they stop when StyleSharp.Analyzers is removed: {string.Join(", ", own.Take(40))}{(own.Count > 40 ? $" and {own.Count - 40} more" : string.Empty)}.");
        }

        if (uncovered.Count == 0)
        {
            return;
        }

        var mapping = LoadMapping();
        var titles = StyleCopSetup.Rules.ToDictionary(r => r.Id, r => r.Title);
        var groups = uncovered.GroupBy(id =>
            result.Reasons.TryGetValue(id, out var why) && why == Migration.CSharpierReason ? Migration.CSharpierReason + " (the repository formats with CSharpier)"
            : result.Reasons.ContainsKey(id) ? "Partly covered or not expressible"
            : mapping.TryGetValue(id, out var proposal) && (proposal.StartsWith("Drop", StringComparison.Ordinal) || proposal.StartsWith("Not applicable", StringComparison.Ordinal)) ? "Dropped by design (no safe automatic fix, or not applicable)"
            : "Not covered yet");
        Console.WriteLine($"Not enforced after removing StyleCop ({uncovered.Count}):");
        foreach (var group in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"  {group.Key}:");
            foreach (var id in group)
            {
                var reason = group.Key.StartsWith(Migration.CSharpierReason, StringComparison.Ordinal) ? titles[id]
                    : result.Reasons.TryGetValue(id, out var r) ? r
                    : group.Key.StartsWith("Dropped", StringComparison.Ordinal) ? UserFacingReason(mapping[id])
                    : titles[id];
                Console.WriteLine($"    {id}{(reason.Length == 0 ? string.Empty : ": " + reason)}");
            }
        }
    }
}
