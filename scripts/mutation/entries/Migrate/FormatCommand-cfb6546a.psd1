# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Append(version ?? string.Empty).Append(Versions(root))'
    Replace = '.Append(version ?? string.Empty)'
    Tests = 'MigrationTests.Verify_RecordKey_HoldsTheSdkAndPackageVersions'
}
