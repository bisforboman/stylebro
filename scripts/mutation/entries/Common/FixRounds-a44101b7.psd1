# Format host: renames get rounds of their own, last
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = 'ordered.Where(i => !interactions.Renames(i))'
    Replace = 'ordered.Where(i => true)'
    Tests = 'FixInteractionsTests.TheRounds_KeepTheOrder_OfIdsThatInteract'
}
