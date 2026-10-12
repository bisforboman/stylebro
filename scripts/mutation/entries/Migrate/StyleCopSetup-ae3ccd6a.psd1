# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '.FirstOrDefault(d => d.Success && d.Groups["v"].Value.Length > 0)'
    Replace = '.FirstOrDefault(d => d.Success)'
    Tests = 'MigrationTests.PackageVersions_FromProperties'
}
