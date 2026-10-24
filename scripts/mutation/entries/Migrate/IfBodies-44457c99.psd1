# Only a region line some parses compile and others leave out keeps its indentation.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = '!regions.TryAdd(line, active) && regions[line] != active'
    Replace = '!regions.TryAdd(line, active)'
    Tests = 'MigrationTests.IfBodies_AlsoAreTheRegionLinesOnlySomeBuildsCompile'
}
