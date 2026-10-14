# Format speed (2026-10-14): no analyzer pass for IDE rules alone
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '"analyzers" => diagnostics.Any(d => !IsStyle(d)),'
    Replace = '"analyzers" => true,'
    Tests = 'MigrationTests.Verify_FormatsOnlyTheFilesWithFindings_InTheCopy'
}
