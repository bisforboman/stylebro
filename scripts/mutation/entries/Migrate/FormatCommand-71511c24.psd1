# with --build the Not-verified verdict comes after the builds (Newtonsoft.Json.Bson, trial 57)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| (buildAll && line.StartsWith(NotVerifiedMarker, StringComparison.Ordinal)))'
    Replace = ')'
    Tests = 'MigrationTests.CheckBuild_SaysNotVerifiedAfterTheBuilds'
}
