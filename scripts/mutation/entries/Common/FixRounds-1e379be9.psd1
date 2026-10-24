# Format host: a fix that adds a document isn't merged
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = 'if (project.GetAddedDocuments().Any() || '
    Replace = 'if ('
    Tests = 'FixInteractionsTests.ARound_MergesEditsThatDontMeet_AndDefersTheRest'
}
