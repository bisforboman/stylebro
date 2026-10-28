# Only the held-back note, not a gap that counts, lets the record stand.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Coverage.cs'
    Find = 'IsGapLine(line) && line.EndsWith(HeldBackReason, StringComparison.Ordinal);'
    Replace = 'IsGapLine(line);'
    Tests = 'MigrationTests.Verify_AnswersFromTheRecord_WithTheHeldBackNote_ButNotWithAGapThatCounts'
}
