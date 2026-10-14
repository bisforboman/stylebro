# Format speed (2026-10-14): whitespace findings name no rule
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.Where(l => IsFindingLine(l) && !IsWhitespaceLine(l))'
    Replace = 'lines.Where(l => IsFindingLine(l))'
    Tests = 'MigrationTests.Verify_FormatsOnlyTheFilesWithFindings_InTheCopy'
}
