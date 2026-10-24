using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace StyleBro.Host;

/// <summary>
/// A run on a copy of the repository (bro's check formats one to tell kept findings apart) in the repository's loaded
/// workspace: the paths are the repository's (<see cref="Source"/>), the files are read from and written to the copy
/// (<see cref="Copy"/>), so the copy needs no load of its own, and what the repository's runs analyzed carries over.
/// </summary>
public sealed class CopyOf(string copy, string source)
{
    /// <summary>Gets the copy's folder, with a separator at the end.</summary>
    public string Copy { get; } = copy;

    /// <summary>Gets the repository's folder the copy was made of, with a separator at the end.</summary>
    public string Source { get; } = source;

    /// <summary>The copy's file for a path in the repository (a path outside it stays as it is).</summary>
    public string OnDisk(string path) => path.StartsWith(this.Source, StringComparison.Ordinal) ? this.Copy + path.Substring(this.Source.Length) : path;

    /// <summary>
    /// Whether <paramref name="solution"/> has a document whose file the copy lacks (git ignores it: the copy has git's files),
    /// other than what the build writes next to a project's intermediate assembly (obj/Debug/net8.0: generated assembly info,
    /// global usings, XAML code), which a load of the copy writes there too.
    /// </summary>
    public bool Lacks(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            var built = Path.GetDirectoryName(project.CompilationOutputInfo.AssemblyPath) is { Length: > 0 } folder ? folder + Path.DirectorySeparatorChar : null;
            foreach (var path in project.Documents.Select(d => d.FilePath).OfType<string>())
            {
                if (!(built is not null && path.StartsWith(built, StringComparison.Ordinal)) && !File.Exists(this.OnDisk(path)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
