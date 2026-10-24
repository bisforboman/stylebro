# Format host: an interaction holds both ways
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = 'With(with, other).Add(words[0]);'
    Replace = ''
    Tests = 'FixInteractionsTests.Ids_InteractWithinACase_AndUnknownOnesWithAll'
}
