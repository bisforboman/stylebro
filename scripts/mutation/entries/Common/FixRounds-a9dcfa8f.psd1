# Format host: a fix that changes an encoding isn't merged
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = '!Equals(oldText.Encoding, newText.Encoding) || '
    Replace = ''
    Tests = 'FixInteractionsTests.ARound_MergesEditsThatDontMeet_AndDefersTheRest'
}
