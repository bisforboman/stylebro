# The record says the note again, as the check does.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = '? [.. notes ?? [], "Clean."]'
    Replace = '? ["Clean."]'
    Tests = 'MigrationTests.Verify_AnswersFromTheRecord_WithTheHeldBackNote_ButNotWithAGapThatCounts'
}
