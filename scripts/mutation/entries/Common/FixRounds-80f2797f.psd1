# Format host: an id the interactions list doesn't name interacts with every id
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = '!this.Knows(a) || !this.Knows(b) || '
    Replace = ''
    Tests = 'FixInteractionsTests.Ids_InteractWithinACase_AndUnknownOnesWithAll'
}
