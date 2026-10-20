@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'if (node is BlockSyntax)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.IfBodies_AlsoAreTheLinesInABlockOnlySomeBuildsOpen'
}
