using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace StyleBro.Host;

/// <summary>What differs between the SDKs' dotnet-format in the members the host calls.</summary>
public static class SdkShapes
{
    /// <summary>
    /// The project argument of CodeFormatter.DetermineFormattableFilesAsync, whose parameter is <paramref name="parameter"/>:
    /// SDK 10 takes the project's path (empty for a solution), SDK 11 the project's id (null for a solution, and for a path
    /// <paramref name="solution"/> doesn't hold).
    /// </summary>
    public static object? ProjectArgument(Type parameter, Solution solution, string projectPath)
    {
        if (parameter.FullName != typeof(ProjectId).FullName)
        {
            return projectPath;
        }

        return projectPath.Length == 0
            ? null
            : solution.Projects.FirstOrDefault(p => string.Equals(p.FilePath, projectPath, StringComparison.OrdinalIgnoreCase))?.Id;
    }
}
