# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '&& Depth(insertAt) == 0;'
    Replace = ';'
    Tests = 'UsingPlacementTests'
}
