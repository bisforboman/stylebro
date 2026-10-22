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

    /// <summary>
    /// The projects <paramref name="selections"/> name whose instance for the named framework the workspace doesn't have
    /// (it didn't load: Serilog's net462 on Linux, where its reference assemblies are missing).
    /// </summary>
    public static HashSet<string> Missing(Solution solution, IReadOnlyList<string> selections)
    {
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var selection in selections)
        {
            var bar = selection.LastIndexOf('|');
            if (bar < 0 || bar == selection.Length - 1)
            {
                continue;
            }

            var (path, framework) = (selection.Substring(0, bar), selection.Substring(bar + 1));
            var instances = solution.Projects.Where(p => string.Equals(p.FilePath, path, StringComparison.OrdinalIgnoreCase)).ToList();
            if (instances.Count > 0 && !(instances.Count == 1 && FrameworkOf(instances[0].Name) is null)
                && !instances.Any(p => string.Equals(FrameworkOf(p.Name), framework, StringComparison.OrdinalIgnoreCase)))
            {
                missing.Add(path);
            }
        }

        return missing;
    }

    /// <summary>The framework of a project MSBuildWorkspace loaded once per framework ('Lib(net8.0)'), else null.</summary>
    public static string? FrameworkOf(string name) =>
        name.EndsWith(")", StringComparison.Ordinal) && name.LastIndexOf('(') is var open and > 0 ? name.Substring(open + 1, name.Length - open - 2) : null;

    /// <summary>
    /// The workspace's load warnings a view has: one about a project (a ProjectDiagnostic) when the view has that project;
    /// otherwise those whose text names one of the view's projects, and those that name none of the workspace's (a project
    /// that didn't load at all), which every view has. "Found project reference without a matching metadata reference: X"
    /// names the project referenced, not the one it is about: by text, it went to every view that has X (System.IO.Abstractions:
    /// the net10.0 run instead of the net472 one). A failure naming a project that did load is about one of its instances that
    /// didn't ("Msbuild failed when processing the file '.../Serilog.csproj'": its net462 instance on Linux): it goes to the views
    /// whose run asks for an instance that isn't there (<paramref name="missing"/>, <see cref="Missing"/>), as a 'dotnet format'
    /// run of that framework alone reports it.
    /// </summary>
    public static List<WorkspaceDiagnostic> WarningsOf(IEnumerable<WorkspaceDiagnostic> diagnostics, Solution workspace, Solution view, IReadOnlyCollection<string>? missing = null)
    {
        missing ??= [];
        var all = workspace.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var own = view.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        bool Names(WorkspaceDiagnostic d, IEnumerable<string> paths) => paths.Any(p => d.Message.Contains(p, StringComparison.OrdinalIgnoreCase));
        return diagnostics.Where(d => d is ProjectDiagnostic project ? view.ContainsProject(project.ProjectId)
            : !Names(d, all) || Names(d, missing) || (d.Kind != WorkspaceDiagnosticKind.Failure && Names(d, own))).ToList();
    }
}
