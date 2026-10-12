# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return version.Success && !version.Groups[1].Value.Contains("$(", StringComparison.Ordinal)'
    Replace = 'return version.Success'
    Tests = 'MigrationTests.PackageVersions_FromProperties'
}
