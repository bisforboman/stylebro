# Format host: an id's round comes after the rounds of the ids before it that it interacts with
@{
    File = 'src/StyleBro.FormatHost/FixRounds.cs'
    Find = 'placed.Where(p => interactions.Interact(p.Id, id))'
    Replace = 'placed.Where(p => false)'
    Tests = 'FixInteractionsTests.TheRounds_KeepTheOrder_OfIdsThatInteract'
}
