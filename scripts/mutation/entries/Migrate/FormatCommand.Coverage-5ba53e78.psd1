# only clean builds checked the code
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = ': buildsClean ? line.Substring'
    Replace = ': true ? line.Substring'
    Tests = 'MigrationTests.CheckBuild_SaysNotVerifiedAfterTheBuilds'
}
