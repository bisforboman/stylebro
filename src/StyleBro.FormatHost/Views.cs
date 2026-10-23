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
    /// <paramref name="names"/> gets the instance names to write as a single-framework load names them. A reference of one of
    /// the run's framework instances to another instance of a project the run names goes to the named one, as in a
    /// 'dotnet format' run of that framework, which loads one instance per project: MSBuildWorkspace sends a reference it can't
    /// match by output to the project's first instance (SmartFormat, trial 47: its net462 projects referenced the libraries'
    /// netstandard2.0 instances, and the analyzers reported those instances' copies of the files, which the whitespace
    /// formatter had left alone: findings at the positions before formatting).
    /// </summary>
    public static Solution Of(Solution solution, IReadOnlyList<string> selections, Dictionary<string, string> names)
    {
        if (selections.Count == 0)
        {
            return solution;
        }

        var roots = RootsOf(solution, selections);
        var frameworks = FrameworksOf(selections);
        var named = roots.Where(p => p.FilePath is not null).GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single().Id, StringComparer.OrdinalIgnoreCase);
        var rootIds = roots.Select(p => p.Id).ToHashSet();
        var retargeted = new List<(ProjectId Project, ProjectReference From, ProjectId To)>();
        var keep = new HashSet<ProjectId>();
        var stack = new Stack<Project>(roots);
        while (stack.Count > 0)
        {
            var project = stack.Pop();
            if (keep.Add(project.Id))
            {
                var ofTheRun = rootIds.Contains(project.Id) || (FrameworkOf(project.Name) is { } own && frameworks.Contains(own));
                foreach (var reference in project.ProjectReferences)
                {
                    if (solution.GetProject(reference.ProjectId) is not { } referenced)
                    {
                        continue;
                    }

                    if (ofTheRun && referenced.FilePath is { } path && named.TryGetValue(path, out var instance) && instance != referenced.Id)
                    {
                        retargeted.Add((project.Id, reference, instance));
                        referenced = solution.GetProject(instance)!;
                    }

                    stack.Push(referenced);
                }
            }
        }

        // A framework's run loads the projects that target it as single-framework builds, named without the framework.
        foreach (var project in solution.Projects.Where(p => keep.Contains(p.Id) && FrameworkOf(p.Name) is { } f && frameworks.Contains(f)))
        {
            names[project.Name] = project.Name.Substring(0, project.Name.Length - FrameworkOf(project.Name)!.Length - 2);
        }

        var view = solution;
        foreach (var (project, from, to) in retargeted)
        {
            view = view.RemoveProjectReference(project, from);
            if (!view.GetProject(project)!.ProjectReferences.Any(r => r.ProjectId == to))
            {
                view = view.AddProjectReference(project, new ProjectReference(to, from.Aliases, from.EmbedInteropTypes));
            }
        }

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
    /// The workspace's load warnings a view has: one about a project (a ProjectDiagnostic) when the view has that project;
    /// otherwise those whose text names one of the view's projects, and those that name none of the workspace's (a project
    /// that didn't load at all), which every view has. "Found project reference without a matching metadata reference: X"
    /// names the project referenced, not the one it is about: by text, it went to every view that has X (System.IO.Abstractions:
    /// the net10.0 run instead of the net472 one). A failure about one framework's instance of a multi-targeted project
    /// ("Msbuild failed when processing the file '.../Serilog.csproj'": its net462 instance on Linux) leaves that instance in
    /// the workspace without a framework in its name ('Serilog' next to 'Serilog(net10.0)'): it goes to the views of runs whose
    /// <paramref name="frameworks"/> none of the project's loaded instances has (Serilog's net462 run, whose TestDummies(net462)
    /// gets Serilog(netstandard2.0)), as a 'dotnet format' run of that framework alone reports it, not to every view with
    /// another instance of the project.
    /// </summary>
    public static List<WorkspaceDiagnostic> WarningsOf(IEnumerable<WorkspaceDiagnostic> diagnostics, Solution workspace, Solution view, IReadOnlyCollection<string>? frameworks = null)
    {
        frameworks ??= [];
        var all = workspace.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var own = view.Projects.Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var failed = Failed(workspace);
        var notLoaded = failed.Where(p => own.Contains(p, StringComparer.OrdinalIgnoreCase) && frameworks.Count > 0
            && !workspace.Projects.Any(x => string.Equals(x.FilePath, p, StringComparison.OrdinalIgnoreCase) && FrameworkOf(x.Name) is { } f && frameworks.Contains(f, StringComparer.OrdinalIgnoreCase))).ToList();
        bool Names(WorkspaceDiagnostic d, IEnumerable<string> paths) => paths.Any(p => d.Message.Contains(p, StringComparison.OrdinalIgnoreCase));
        return diagnostics.Where(d => d is ProjectDiagnostic project
            ? view.ContainsProject(project.ProjectId)
            : !Names(d, all) || Names(d, notLoaded) || (!(d.Kind == WorkspaceDiagnosticKind.Failure && Names(d, failed)) && Names(d, own))).ToList();
    }

    /// <summary>The frameworks <paramref name="selections"/> ('path|framework') name.</summary>
    public static HashSet<string> FrameworksOf(IEnumerable<string> selections) =>
        new(selections.Select(s => s.Substring(s.LastIndexOf('|') + 1)).Where(f => f.Length > 0 && !f.Contains('/') && !f.Contains('\\')), StringComparer.OrdinalIgnoreCase);

    // The instances the selections ('path|framework') name.
    private static List<Project> RootsOf(Solution solution, IReadOnlyList<string> selections)
    {
        var byPath = solution.Projects.Where(p => p.FilePath is not null).ToLookup(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);
        var roots = new List<Project>();
        foreach (var selection in selections)
        {
            var bar = selection.LastIndexOf('|');
            var (path, framework) = bar < 0 ? (selection, string.Empty) : (selection.Substring(0, bar), selection.Substring(bar + 1));
            var instances = byPath[path].ToList();
            roots.AddRange(framework.Length == 0 || (instances.Count == 1 && FrameworkOf(instances[0].Name) is null)
                ? instances
                : instances.Where(p => string.Equals(FrameworkOf(p.Name), framework, StringComparison.OrdinalIgnoreCase)));
        }

        return roots;
    }

    // The projects with an instance named without a framework next to instances named with one: a framework's that didn't load.
    private static List<string> Failed(Solution workspace) =>
        workspace.Projects.Where(p => p.FilePath is not null).GroupBy(p => p.FilePath!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Any(p => FrameworkOf(p.Name) is null) && g.Any(p => FrameworkOf(p.Name) is not null))
            .Select(g => g.Key)
            .ToList();
}
