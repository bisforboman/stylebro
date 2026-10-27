# and says what the builds checked
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(workspaceForBuild is null ? line : AfterBuilds(line, buildLine is null))'
    Replace = 'line'
    Tests = 'MigrationTests.CheckBuild_SaysNotVerifiedAfterTheBuilds'
}
