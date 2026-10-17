# Trial 28: an existing baseline next to the solution's projects is found
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'for (var folder = below ? at : Path.GetFullPath(root); folder is not null;'
    Replace = 'for (var folder = Path.GetFullPath(root); folder is not null;'
    Tests = 'MigrationTests.Baseline_GoesNextToTheSolutionsProjects_WhicheverWayFormatRuns'
}
