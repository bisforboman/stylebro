# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = ' && !aboveIsOpenBrace)'
    Replace = ')'
    Tests = 'UsingPlacementTests'
}
