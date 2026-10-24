# Format host: edits that only meet at a position count as touching
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = 'n.Span.Start <= t.Span.End'
    Replace = 'n.Span.Start < t.Span.End'
    Tests = 'FixInteractionsTests.ARound_MergesEditsThatDontMeet_AndDefersTheRest'
}
