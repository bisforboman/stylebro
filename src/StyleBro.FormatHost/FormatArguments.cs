using System;
using System.Collections.Generic;

namespace StyleBro.Host;

/// <summary>
/// A 'dotnet format' command line as the host reads it ('format' already left out): the pass, the workspace and the options
/// bro passes. Plain types only, so the tests read it without the SDK's assemblies.
/// </summary>
public sealed class FormatArguments
{
    /// <summary>The passes 'dotnet format' has besides the plain one ("").</summary>
    public static readonly string[] Passes = ["whitespace", "style", "analyzers"];

    private FormatArguments(string pass, string target)
    {
        this.Pass = pass;
        this.Target = target;
    }

    /// <summary>"whitespace", "style", "analyzers", or "" for a plain 'dotnet format' (all three).</summary>
    public string Pass { get; }

    /// <summary>Gets the solution, solution filter or project the command names.</summary>
    public string Target { get; }

    /// <summary>Gets a value indicating whether it only checks (--verify-no-changes): the host runs nothing else in phase 1.</summary>
    public bool Verify { get; private set; }

    /// <summary>--no-restore.</summary>
    public bool NoRestore { get; private set; }

    /// <summary>--include-generated.</summary>
    public bool IncludeGenerated { get; private set; }

    /// <summary>--include's paths and globs, relative to the working folder.</summary>
    public List<string> Include { get; } = [];

    /// <summary>--exclude's paths and globs.</summary>
    public List<string> Exclude { get; } = [];

    /// <summary>--diagnostics.</summary>
    public List<string> Diagnostics { get; } = [];

    /// <summary>--exclude-diagnostics.</summary>
    public List<string> ExcludeDiagnostics { get; } = [];

    /// <summary>--severity's value, or null.</summary>
    public string? Severity { get; private set; }

    /// <summary>--verbosity's value, or null.</summary>
    public string? Verbosity { get; private set; }

    /// <summary>--report's path ("" when it has none), or null without --report.</summary>
    public string? Report { get; private set; }

    /// <summary>Gets a value indicating whether the command names a solution (or a filter of one) rather than a project.</summary>
    public bool IsSolution => this.Target.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || this.Target.EndsWith(".slnf", StringComparison.OrdinalIgnoreCase)
        || this.Target.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads <paramref name="arguments"/>, or null for a command the host doesn't run the way 'dotnet format' would (an option
    /// it doesn't know, --folder, --binarylog, no workspace): bro starts 'dotnet format' for that one.
    /// </summary>
    public static FormatArguments? Parse(IReadOnlyList<string> arguments)
    {
        var at = 0;
        var pass = at < arguments.Count && Array.IndexOf(Passes, arguments[at]) >= 0 ? arguments[at++] : string.Empty;
        if (at >= arguments.Count || arguments[at].StartsWith("-", StringComparison.Ordinal))
        {
            return null;
        }

        var parsed = new FormatArguments(pass, arguments[at++]);
        while (at < arguments.Count)
        {
            var option = arguments[at++];
            switch (option)
            {
                case "--verify-no-changes":
                    parsed.Verify = true;
                    break;

                case "--no-restore":
                    parsed.NoRestore = true;
                    break;

                case "--include-generated":
                    parsed.IncludeGenerated = true;
                    break;

                case "--include":
                    at = Values(arguments, at, parsed.Include);
                    break;

                case "--exclude":
                    at = Values(arguments, at, parsed.Exclude);
                    break;

                case "--diagnostics":
                    at = Values(arguments, at, parsed.Diagnostics);
                    break;

                case "--exclude-diagnostics":
                    at = Values(arguments, at, parsed.ExcludeDiagnostics);
                    break;

                case "--severity" when at < arguments.Count:
                    parsed.Severity = arguments[at++];
                    break;

                case "--verbosity" or "-v" when at < arguments.Count:
                    parsed.Verbosity = arguments[at++];
                    break;

                case "--report":
                    parsed.Report = at < arguments.Count && !arguments[at].StartsWith("-", StringComparison.Ordinal) ? arguments[at++] : string.Empty;
                    break;

                default:
                    return null;
            }
        }

        return parsed.Severity is null or "info" or "warn" or "error" or "hidden" && LogLevelOf(parsed.Verbosity) >= 0 ? parsed : null;
    }

    /// <summary>
    /// The log level 'dotnet format' shows from (Microsoft.Extensions.Logging's numbers: 0 trace ... 4 error), or -1 for a
    /// verbosity it doesn't take. Without --verbosity: warnings.
    /// </summary>
    public static int LogLevelOf(string? verbosity) => verbosity switch
    {
        null => 3,
        "q" or "quiet" => 4,
        "m" or "minimal" => 3,
        "n" or "normal" => 2,
        "d" or "detailed" => 1,
        "diag" or "diagnostic" => 0,
        _ => -1,
    };

    /// <summary>The fix categories of a pass, as dotnet format's flags: 1 whitespace, 2 code style, 4 analyzers.</summary>
    public static int Categories(string pass) => pass switch
    {
        "whitespace" => 1,
        "style" => 2,
        "analyzers" => 4,
        _ => 7,
    };

    /// <summary>A severity as Roslyn's DiagnosticSeverity numbers (hidden 0 ... error 3); warnings without one.</summary>
    public static int SeverityOf(string? severity) => severity switch
    {
        "error" => 3,
        "info" => 1,
        "hidden" => 0,
        _ => 2,
    };

    // An option's values: the arguments up to the next option.
    private static int Values(IReadOnlyList<string> arguments, int at, List<string> values)
    {
        while (at < arguments.Count && !arguments[at].StartsWith("-", StringComparison.Ordinal))
        {
            values.Add(arguments[at++]);
        }

        return at;
    }
}
