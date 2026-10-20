# check --build names projects a failed one kept from compiling (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'Uncompiled(output, current, expectedProjects ??= [.. expected?.Invoke() ?? BuiltProjects(workspacePath, root)]) is { Count: > 0 } skipped'
    Replace = 'new List<string>() is { Count: > 0 } skipped'
    Tests = 'BuildCheck_AProjectAFailedOneKeptFromCompiling'
}
