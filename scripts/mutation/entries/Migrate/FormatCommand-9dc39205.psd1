# The held-back note alone still lets a clean format record its check (trial 58).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!incomplete && gaps.All(g => IsNoteGap(g) || IsRawStringGap(g));'
    Replace = '!incomplete && gaps.Count == 0;'
    Tests = 'MigrationTests.Verify_AnswersFromTheRecord_WithTheHeldBackNote_ButNotWithAGapThatCounts'
}
