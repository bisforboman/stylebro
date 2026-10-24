# Format host: a fix whose edits meet another's in a merged round runs in a round of its own
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = '|| edits.Any(e => taken.TryGetValue(e.Key, out var before) && Touch(before, e.Value))'
    Replace = ''
    Tests = 'FixInteractionsTests.ARound_MergesEditsThatDontMeet_AndDefersTheRest'
}
