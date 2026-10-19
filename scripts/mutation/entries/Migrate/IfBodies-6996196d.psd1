@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = ' || OpensBlock(text)'
    Replace = ''
    Tests = 'MigrationTests.IfBodies_AlsoAreTheLinesInABlockOnlySomeBuildsOpen'
}
