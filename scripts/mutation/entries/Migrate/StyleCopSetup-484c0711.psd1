# Trial 12 (2026-10-12): Steeltoe, sourcegit
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = '.Select(v => (Raw: v, Value: ResolveProperties(v, properties)))'
    Replace = '.Select(v => (Raw: v, Value: (string?)v))'
    Tests = 'MigrationTests.PackageVersions_FromProperties'
}
