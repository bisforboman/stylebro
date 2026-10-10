using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using StyleBro.Analyzers;

namespace StyleBro.Migrate;

/// <summary>
/// '--json': one JSON object on stdout with what the command did (owner's request 2026-10-10, docs/agents.md documents every
/// field); the human text goes to stderr. The commands record into <see cref="Current"/> while it's set; without --json
/// nothing is recorded.
/// </summary>
internal static class JsonReport
{
    /// <summary>The option.</summary>
    public const string Option = "--json";

    /// <summary>The schema's version: raised when a field changes meaning or goes away (new fields don't raise it).</summary>
    public const int SchemaVersion = 1;

    private static readonly AsyncLocal<JsonObject?> Report = new();

    /// <summary>Gets the report being written, or null without --json.</summary>
    public static JsonObject? Current => Report.Value;

    /// <summary>Starts a report: every field the schema has, empty.</summary>
    public static JsonObject Begin(string command, string? previewOf = null)
    {
        Report.Value = new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["command"] = command,
            ["previewOf"] = previewOf,
            ["path"] = null,
            ["write"] = false,
            ["settings"] = new JsonArray(),
            ["conventions"] = new JsonArray(),
            ["sonar"] = null,
            ["agentsMd"] = null,
            ["runs"] = new JsonArray(),
            ["changesPerRule"] = new JsonObject(),
            ["filesPerRule"] = null,
            ["keptFindings"] = new JsonArray(),
            ["patch"] = null,
            ["clean"] = null,
            ["exitCode"] = null,
        };
        return Report.Value;
    }

    /// <summary>Ends the report: the exit code (and 'clean' when the command didn't set it) and the JSON text.</summary>
    public static string End(int exitCode)
    {
        var report = Report.Value ?? Begin("unknown");
        report["exitCode"] = exitCode;
        Report.Value = null;
        return report.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    /// <summary>Sets a field.</summary>
    public static void Set(string key, JsonNode? value)
    {
        if (Current is { } report)
        {
            report[key] = value;
        }
    }

    /// <summary>Adds to an array field.</summary>
    public static void Add(string key, JsonNode item)
    {
        if (Current?[key] is JsonArray array)
        {
            array.Add(item);
        }
    }

    /// <summary>
    /// The settings of an .editorconfig block (between stylebro markers): every 'key = value' line with its section and the
    /// comment lines right above it (why).
    /// </summary>
    public static void AddSettings(string file, string block)
    {
        if (Current is null)
        {
            return;
        }

        var section = string.Empty;
        var why = new List<string>();
        var afterSetting = false;
        foreach (var raw in block.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("# BEGIN", StringComparison.Ordinal) || line.StartsWith("# END", StringComparison.Ordinal) || line.Length == 0)
            {
                why.Clear();
            }
            else if (line.StartsWith('#'))
            {
                // Settings right below each other share the comment above them; a new comment starts a new reason.
                if (afterSetting)
                {
                    why.Clear();
                }

                why.Add(line.TrimStart('#', ' '));
                afterSetting = false;
                continue;
            }
            else if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line.Substring(1, line.Length - 2);
                why.Clear();
            }
            else if (line.IndexOf('=') is var equals and > 0)
            {
                Add("settings", new JsonObject
                {
                    ["file"] = file.Replace('\\', '/'),
                    ["section"] = section,
                    ["key"] = line.Substring(0, equals).Trim(),
                    ["value"] = line.Substring(equals + 1).Trim(),
                    ["why"] = why.Count == 0 ? null : string.Join(" ", why),
                });
                afterSetting = true;
                continue;
            }

            afterSetting = false;
        }
    }

    /// <summary>The kept findings (full paths; written relative to the report's path).</summary>
    public static void AddKept(IEnumerable<KeptFinding> kept)
    {
        foreach (var finding in kept)
        {
            Add("keptFindings", new JsonObject
            {
                ["path"] = Relative(finding.Path),
                ["line"] = finding.Line,
                ["column"] = finding.Column,
                ["rule"] = finding.Id,
                ["reason"] = finding.Reason.ToString(),
                ["reasonText"] = KeptFinding.Describe(finding.Reason),
                ["message"] = finding.Message,
            });
        }
    }

    /// <summary>
    /// A format run: the files it changed (full paths) and the changes per rule from its format-report.json (when there is
    /// one), also added to the totals.
    /// </summary>
    public static void AddRun(int run, IEnumerable<string> changed, string? reportFile)
    {
        if (Current is not { } report)
        {
            return;
        }

        var perRule = reportFile is not null && File.Exists(reportFile) ? ChangesPerRule(File.ReadAllText(reportFile)) : new SortedDictionary<string, int>(StringComparer.Ordinal);
        var totals = (JsonObject)report["changesPerRule"]!;
        foreach (var (id, count) in perRule)
        {
            totals[id] = (totals[id]?.GetValue<int>() ?? 0) + count;
        }

        Add("runs", new JsonObject
        {
            ["run"] = run,
            ["filesChanged"] = new JsonArray(changed.Select(f => (JsonNode?)Relative(f)).ToArray()),
            ["changesPerRule"] = new JsonObject(perRule.Select(p => KeyValuePair.Create(p.Key, (JsonNode?)p.Value))),
        });
    }

    /// <summary>The changes in a format-report.json per rule ('WHITESPACE' counts as IDE0055), each change once.</summary>
    public static SortedDictionary<string, int> ChangesPerRule(string reportJson)
    {
        var seen = new HashSet<(string, string, int, int)>();
        using var report = JsonDocument.Parse(reportJson);
        foreach (var document in report.RootElement.EnumerateArray())
        {
            var file = document.TryGetProperty("FilePath", out var path) ? path.GetString() ?? string.Empty : string.Empty;
            foreach (var change in document.GetProperty("FileChanges").EnumerateArray())
            {
                var id = change.GetProperty("DiagnosticId").GetString() ?? "other";
                seen.Add((id == "WHITESPACE" ? "IDE0055" : id, file.ToUpperInvariant(), change.GetProperty("LineNumber").GetInt32(), change.GetProperty("CharNumber").GetInt32()));
            }
        }

        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (id, _, _, _) in seen)
        {
            counts[id] = counts.TryGetValue(id, out var n) ? n + 1 : 1;
        }

        return counts;
    }

    /// <summary>A full path relative to the report's path (the folder the command ran on), with '/'.</summary>
    public static string Relative(string path) =>
        (Current?["path"]?.GetValue<string>() is { } root ? Path.GetRelativePath(root, path) : path).Replace('\\', '/');
}
