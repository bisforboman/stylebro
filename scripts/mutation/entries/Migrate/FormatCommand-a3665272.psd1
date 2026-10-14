# Format speed (2026-10-14): no style pass without an IDE rule
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '"style" => diagnostics.Any(IsStyle),'
    Replace = '"style" => true,'
    Tests = 'MigrationTests.Verify_FormatsOnlyTheFilesWithFindings_InTheCopy'
}
