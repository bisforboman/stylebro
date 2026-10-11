# Trial 8, tool (2026-10-11): an import through a property the file defines (Orleans' _ParentDirectoryBuildPropsPath)
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (value.Contains("$(", StringComparison.Ordinal))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.AddPackage_FollowsANestedPropsImport'
}
