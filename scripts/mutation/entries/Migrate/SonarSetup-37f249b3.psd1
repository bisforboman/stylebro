# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '{(version == "?" ? "(version unknown)" : version)}'
    Replace = '{version}'
    Tests = 'MigrationTests.PackageVersions_FromProperties'
}
