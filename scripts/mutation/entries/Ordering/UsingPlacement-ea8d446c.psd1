# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'if (startLine == 0 && mode == UsingPlacementMode.Inside)'
    Replace = 'if (false)'
    Tests = 'UsingPlacementTests'
}
