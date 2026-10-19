@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (level > 0 && braces != 0)'
    Replace = 'if (level > 0)'
    Tests = 'MigrationTests.IfBodies_AlsoAreTheLinesInABlockOnlySomeBuildsOpen'
}
