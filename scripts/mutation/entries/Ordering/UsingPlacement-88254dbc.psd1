# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = ': !g.Find(name).IsSubsetOf(fromU))'
    Replace = ': false)'
    Tests = 'UsingPlacementTests'
}
