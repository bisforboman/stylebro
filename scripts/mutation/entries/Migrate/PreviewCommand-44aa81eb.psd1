# Trial 31: a packages folder outside the repository is not excluded
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'return relative is "." or "" || relative.StartsWith("../", StringComparison.Ordinal)'
    Replace = 'return relative is "." or ""'
    Tests = 'MigrationTests.Preview_LeavesAPackagesFolder'
}
