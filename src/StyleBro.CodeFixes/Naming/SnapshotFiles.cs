using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// The words of a solution's snapshot and approval files (Verify's '*.verified.*' and '*.received.*', ApprovalTests'
/// '*.approved.*'): they hold the names a serializer wrote, so they're treated like strings (owner's choice 2026-10-15).
/// A data member whose name is there keeps it: renaming broke 2 of 15 xunit.v3 + Verify tests in trial 23. Read from the
/// solution's additional files and from disk, under each project's repository (its git root, else its folder), so the
/// tests' snapshots count while a 'stylebro-migrate format' run loads only the library. The same scan reads XAML files
/// ('.xaml': WPF, WinUI, MAUI; '.axaml': Avalonia): the identifiers in their attribute values that can name code
/// ('{Binding title}', 'DisplayMemberPath="label"', 'x:Name', 'Click="OnSave"') bind to members by name at run time, so
/// they're treated like strings too (owner's choice 2026-10-16; trial 26: a renamed view-model property left a WPF binding
/// empty), and its element and attribute names from the project's namespaces name its types and properties.
/// </summary>
internal static class SnapshotFiles
{
    private static readonly Regex Word = new(@"[\p{L}_][\p{L}\p{Nd}_]*", RegexOptions.CultureInvariant);

    // An attribute and its quoted value ('Text="{Binding title}"', "Path='Items.count'").
    private static readonly Regex Attribute = new(@"(?<name>[\w:.]+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')", RegexOptions.CultureInvariant);

    // A namespace declaration that maps a prefix (or the default namespace) to the project's code: WPF/MAUI's
    // 'clr-namespace:', Avalonia's 'using:'.
    private static readonly Regex CodeNamespace = new(@"\bxmlns(?::(?<prefix>[\w.]+))?\s*=\s*[""'](?:clr-namespace:|using:)", RegexOptions.CultureInvariant);

    // A start tag with its attributes ('<local:Badge Caption="hi" />', '<local:Badge.Caption>').
    private static readonly Regex StartTag = new(@"<(?<tag>[\w:.]+)(?<attributes>(?:\s+[\w:.]+\s*=\s*(?:""[^""]*""|'[^']*'))*)\s*/?>", RegexOptions.CultureInvariant);

    // A value that is one name, maybe dotted or prefixed: 'onSave', 'Items.Count', 'local:Badge.Caption'.
    private static readonly Regex NameValue = new(@"^\s*(?:[\p{L}_][\w.]*:)?[\p{L}_][\p{L}\p{Nd}_]*(?:\.[\p{L}_][\p{L}\p{Nd}_]*)*\s*$", RegexOptions.CultureInvariant);

    // A namespace prefix ('x:' in '{x:Static ...}', 'local:' in 'local:Badge'): not a name of code.
    private static readonly Regex Prefix = new(@"(?<![\w.$])[\p{L}_][\p{L}\p{Nd}_]*:", RegexOptions.CultureInvariant);

    // Attributes whose value is text for people ('Text="window width"'): a word there names nothing (trial 28, Text-Grab).
    private static readonly HashSet<string> FreeText = new(StringComparer.Ordinal)
    {
        "Text", "ToolTip", "Content", "Header", "Title", "Description", "PlaceholderText", "Placeholder", "Watermark", "Label", "HelpText",
    };

    private static readonly string[] Markers = { ".verified.", ".approved.", ".received." };

    private static readonly string[] SkippedFolders = { "bin", "obj", ".git", ".vs", "node_modules" };

    // The disk scan per folder, reused for five minutes: 'dotnet format' builds a new solution for every rule it fixes.
    private static readonly Dictionary<string, (DateTime At, Words Words)> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the file name marks a snapshot or approval file ('Tests.Summary.verified.txt').</summary>
    public static bool IsSnapshotFile(string path)
    {
        var name = Path.GetFileName(path);
        return Markers.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) > 0);
    }

    /// <summary>Whether the file is XAML ('MainWindow.xaml', Avalonia's 'App.axaml'), not its code-behind.</summary>
    public static bool IsXamlFile(string path) =>
        path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Every word of the solution's snapshot files, and every identifier in its XAML files' attribute values, with where it
    /// first is ('path(line)').
    /// </summary>
    public static Words GetWords(Solution solution)
    {
        var words = new Words();
        foreach (var document in solution.Projects.SelectMany(p => p.AdditionalDocuments))
        {
            if (document.FilePath is { } path && IsSnapshotFile(path) && document.GetTextAsync().GetAwaiter().GetResult() is { } text)
            {
                Add(words.Snapshots, path, text.ToString());
            }
            else if (document.FilePath is { } xaml && IsXamlFile(xaml) && document.GetTextAsync().GetAwaiter().GetResult() is { } markup)
            {
                AddXaml(words, xaml, markup.ToString());
            }
        }

        foreach (var root in GetRoots(solution))
        {
            var scanned = Scan(root);
            Merge(words.Snapshots, scanned.Snapshots);
            Merge(words.Xaml, scanned.Xaml);
            Merge(words.XamlMarkup, scanned.XamlMarkup);
        }

        return words;

        static void Merge(Dictionary<string, string> into, Dictionary<string, string> from)
        {
            foreach (var pair in from)
            {
                if (!into.ContainsKey(pair.Key))
                {
                    into[pair.Key] = pair.Value;
                }
            }
        }
    }

    /// <summary>The folders to scan: each project's git root, else its folder; none inside another.</summary>
    private static List<string> GetRoots(Solution solution)
    {
        var roots = new List<string>();
        foreach (var folder in solution.Projects.Select(p => Path.GetDirectoryName(p.FilePath ?? string.Empty)).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var root = GitRoot(folder!) ?? folder!;
            if (!roots.Any(r => IsInside(root, r)))
            {
                roots.RemoveAll(r => IsInside(r, root));
                roots.Add(root);
            }
        }

        return roots;

        static bool IsInside(string path, string folder) =>
            path.Equals(folder, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

#pragma warning disable RS1035 // A code fix (not an analyzer) reads the repository's snapshot files, like the renamer's other guards.
    private static string? GitRoot(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }
        }

        return null;
    }

    private static Words Scan(string root)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(root, out var cached) && DateTime.UtcNow - cached.At < TimeSpan.FromMinutes(5))
            {
                return cached.Words;
            }
        }

        var words = new Words();
        var folders = new Stack<string>();
        folders.Push(root);
        while (folders.Count > 0)
        {
            var folder = folders.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    // Binary snapshots (images) and huge ones don't hold member names worth reading.
                    if (IsSnapshotFile(file) && new FileInfo(file).Length < 1 << 20 && File.ReadAllText(file) is var text && text.IndexOf('\0') < 0)
                    {
                        Add(words.Snapshots, file, text);
                    }
                    else if (IsXamlFile(file) && new FileInfo(file).Length < 1 << 20)
                    {
                        AddXaml(words, file, File.ReadAllText(file));
                    }
                }

                foreach (var sub in Directory.EnumerateDirectories(folder))
                {
                    if (!SkippedFolders.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase))
                    {
                        folders.Push(sub);
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A folder that can't be read has no snapshots we could honour.
            }
        }

        lock (Cache)
        {
            Cache[root] = (DateTime.UtcNow, words);
        }

        return words;
    }
#pragma warning restore RS1035

    /// <summary>
    /// The identifiers of a XAML file that name code. <see cref="Words.Xaml"/>: those in attribute values that can name
    /// members (<c>NamesCode</c>): binding paths and their segments, member paths, 'x:Name', event handlers, 'x:Static'
    /// members, without namespace prefixes; not free text (trial 28, Text-Grab: 'Text="... window width"' kept a property
    /// 'width'), nor geometry ('Data="M0,0 z"'). <see cref="Words.XamlMarkup"/>: the element and attribute names from the
    /// project's own namespaces (a prefix mapped to 'clr-namespace:' or Avalonia's 'using:'): its types
    /// ('&lt;local:BoolConverter/&gt;'), their properties ('&lt;local:Badge Caption="hi"/&gt;', '&lt;local:Badge.Caption&gt;')
    /// and attached properties ('local:TagBehavior.Key="main"'). Trial 27: renamed types and properties used there broke
    /// the build (MC3074, MC3072). The framework's element and attribute names are left out.
    /// </summary>
    private static void AddXaml(Words found, string path, string text)
    {
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lineStarts.Add(i + 1);
            }
        }

        void AddWords(Dictionary<string, string> words, string value, int index)
        {
            foreach (Match match in Word.Matches(value))
            {
                if (!words.ContainsKey(match.Value))
                {
                    var line = lineStarts.BinarySearch(index + match.Index);
                    words[match.Value] = $"{path}({(line >= 0 ? line + 1 : ~line)})";
                }
            }
        }

        foreach (Match attribute in Attribute.Matches(text))
        {
            var value = attribute.Groups["value"];
            if (NamesCode(attribute.Groups["name"].Value, value.Value))
            {
                // Prefixes blanked out, so the words keep their places ('{x:Static ...}' keeps no member called 'x').
                AddWords(found.Xaml, Prefix.Replace(value.Value, m => new string(' ', m.Length)), value.Index);
            }
        }

        var local = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match declaration in CodeNamespace.Matches(text))
        {
            local.Add(declaration.Groups["prefix"].Value);
        }

        if (local.Count == 0)
        {
            return;
        }

        foreach (Match tag in StartTag.Matches(text))
        {
            var (prefix, tagName) = Split(tag.Groups["tag"].Value);
            var ownElement = local.Contains(prefix);
            if (ownElement)
            {
                AddWords(found.XamlMarkup, tagName, tag.Groups["tag"].Index);
            }

            var attributes = tag.Groups["attributes"];
            foreach (Match attribute in Attribute.Matches(attributes.Value))
            {
                var fullName = attribute.Groups["name"].Value;
                var (attributePrefix, attributeName) = Split(fullName);

                // 'local:TagBehavior.Key' anywhere; 'Caption' (no prefix) on the project's own element.
                if (!fullName.StartsWith("xmlns", StringComparison.Ordinal)
                    && (attributePrefix.Length > 0 ? local.Contains(attributePrefix) : ownElement || (attributeName.IndexOf('.') >= 0 && local.Contains(string.Empty))))
                {
                    AddWords(found.XamlMarkup, attributeName, attributes.Index + attribute.Index);
                }
            }
        }

        // Values that can name code: markup extensions ('{Binding Path=title}', '{x:Static local:Foo.Bar}', ElementName),
        // 'x:' directives ('x:Name', 'x:Class', 'x:DataType'), 'Name', member paths ('DisplayMemberPath'), and a value that is
        // one name ('Click="OnSave"', 'TargetType="local:Badge"') unless the attribute holds text for people.
        static bool NamesCode(string name, string value)
        {
            if (name.StartsWith("xmlns", StringComparison.Ordinal))
            {
                return false;
            }

            var local = name.Substring(name.IndexOf(':') + 1);
            var trimmed = value.TrimStart();
            return (trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.StartsWith("{}", StringComparison.Ordinal))
                || name.StartsWith("x:", StringComparison.Ordinal)
                || local == "Name"
                || local.EndsWith("Path", StringComparison.Ordinal)
                || (!FreeText.Contains(local.Substring(local.LastIndexOf('.') + 1)) && NameValue.IsMatch(value));
        }

        static (string Prefix, string Name) Split(string name)
        {
            var colon = name.IndexOf(':');
            return colon < 0 ? (string.Empty, name) : (name.Substring(0, colon), name.Substring(colon + 1));
        }
    }

    private static void Add(Dictionary<string, string> words, string path, string text)
    {
        var line = 1;
        var counted = 0;
        foreach (Match match in Word.Matches(text))
        {
            for (; counted < match.Index; counted++)
            {
                line += text[counted] == '\n' ? 1 : 0;
            }

            if (!words.ContainsKey(match.Value))
            {
                words[match.Value] = $"{path}({line})";
            }
        }
    }

    /// <summary>The words of the snapshot files and the XAML files, each with where it first is ('path(line)').</summary>
    internal sealed class Words
    {
        public Dictionary<string, string> Snapshots { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the identifiers in XAML attribute values that can name code (bindings, 'x:Name', event handlers).</summary>
        public Dictionary<string, string> Xaml { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the XAML element and attribute names from the project's own namespaces (its types and properties).</summary>
        public Dictionary<string, string> XamlMarkup { get; } = new(StringComparer.Ordinal);
    }
}
