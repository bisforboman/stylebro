# The held-back note alone still lets a clean format record its check (trial 58).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '!incomplete && gaps.All(IsHeldBackGap);'
    Replace = '!incomplete && gaps.Count == 0;'
    Tests = 'MigrationTests.Verify_AnswersFromTheRecord_WithTheHeldBackNote_ButNotWithAGapThatCounts'
}
