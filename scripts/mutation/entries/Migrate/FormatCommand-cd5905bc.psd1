# Trial 25: files of several projects run together
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!hasPath && groups.Count > 1 && CommonFolder('
    Replace = 'if (!hasPath && groups.Count > 100 && CommonFolder('
    Tests = 'MigrationTests.Files_AreFormattedInTheirNearestProject_OnlyThoseFiles'
}
