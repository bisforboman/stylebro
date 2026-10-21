# Trial 39: only the projects inside the not-built folders leave the runs.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'folders.Any(f => relative.StartsWith(f, StringComparison.OrdinalIgnoreCase))'
    Replace = 'folders.Any(f => !relative.StartsWith(f, StringComparison.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.ProjectsIn_TheNotBuiltFolders_AreLeftOutOfTheRuns'
}
