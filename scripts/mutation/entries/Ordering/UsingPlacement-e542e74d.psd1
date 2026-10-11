# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'return UsingPlacementMode.Preserve;'
    Replace = 'return UsingPlacementMode.Inside;'
    Tests = 'UsingPlacementTests'
}
