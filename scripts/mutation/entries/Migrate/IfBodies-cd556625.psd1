# Only a region line right below another directive moves in a left-out branch.
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (line == 0 || !source.Lines[line - 1].ToString().TrimStart().StartsWith(''#''))'
    Replace = 'if (line == 0)'
    Tests = 'MigrationTests.IfBodies_AlsoAreTheRegionLinesOnlySomeBuildsCompile'
}
