# Trial 28: a new baseline goes next to the solution's projects
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Directory.Exists(root) && BaselineCommand.FindWorkspace(root) is { } workspace && File.Exists(Path.Combine(root, workspace))'
    Replace = 'false && BaselineCommand.FindWorkspace(root) is { } workspace'
    Tests = 'MigrationTests.Baseline_GoesNextToTheSolutionsProjects_WhicheverWayFormatRuns'
}
