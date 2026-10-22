using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace StyleBro.Host;

/// <summary>
/// The part of a configuration's workspace one plan entry checks (<see cref="Session"/>). Only Roslyn's Workspaces API, so
/// the tests run it on their own Roslyn.
/// </summary>
public static class Views
{
    /// <summary>
    /// The view of <paramref name="solution"/> a run checks: the instances <paramref name="selections"/> name ('path|framework',
    /// the framework as that project spells it, '' for all its instances) and every project they reference, transitively.
    /// <paramref name="names"/> gets the instance names to write as a single-framework load names them.
    /// </summary>
    public static Solution Of(Solution solution, IReadOnlyList<string> selections, Dictionary<string, string> names)
    {
        if (selections.Count == 0)
        {
            return solution;
        }

        var byPath = solution.Projects.Where(p => p.FilePath is not null).ToLookup(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);
        var roots = new List<Project>();
        var frameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var selection in selections)
        {
            var bar = selection.LastIndexOf('|');
            var (path, framework) = bar < 0 ? (selection, string.Empty) : (selection.Substring(0, bar), selection.Substring(bar + 1));
            var instances = byPath[path].ToList();
            var chosen = framework.Length == 0 || (instances.Count == 1 && FrameworkOf(instances[0].Name) is null)
                ? instances
                : instances.Where(p => string.Equals(FrameworkOf(p.Name), framework, StringComparison.OrdinalIgnoreCase)).ToList();
            roots.AddRange(chosen);
            if (framework.Length > 0)
            {
                frameworks.Add(framework);
            }
        }

        var keep = new HashSet<ProjectId>();
        var stack = new Stack<Project>(roots);
        while (stack.Count > 0)
        {
            var project = stack.Pop();
            if (keep.Add(project.Id))
            {
                foreach (var reference in project.ProjectReferences)
                {
                    if (solution.GetProject(reference.ProjectId) is { } referenced)
                    {
                        stack.Push(referenced);
                    }
                }
            }
        }

        // A framework's run loads the projects that target it as single-framework builds, named without the framework.
        foreach (var project in solution.Projects.Where(p => keep.Contains(p.Id) && FrameworkOf(p.Name) is { } f && frameworks.Contains(f)))
        {
            names[project.Name] = project.Name.Substring(0, project.Name.Length - FrameworkOf(project.Name)!.Length - 2);
        }

        var view = solution;
        foreach (var project in solution.ProjectIds.Where(id => !keep.Contains(id)))
        {
            view = view.RemoveProject(project);
        }

        return view;
    }

    /// <summary>The framework of a project MSBuildWorkspace loaded once per framework ('Lib(net8.0)'), else null.</summary>
    public static string? FrameworkOf(string name) =>
        name.EndsWith(")", StringComparison.Ordinal) && name.LastIndexOf('(') is var open and > 0 ? name.Substring(open + 1, name.Length - open - 2) : null;

    /// <summary>
    /// The workspace's load warnings a view has: those that name one of its projects, and those that name none of the
    /// workspace's (a project that didn't load at all), which every view has.
    /// </summary>
    public static List<WorkspaceDiagnostic> WarningsOf(IEnumerable<WorkspaceDiagnostic> diagnostics, Solution workspace, Solution view)
    {
        var all = workspace.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var own = view.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return diagnostics.Where(d => own.Any(p => d.Message.Contains(p, StringComparison.OrdinalIgnoreCase)) || !all.Any(p => d.Message.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
    }
}
