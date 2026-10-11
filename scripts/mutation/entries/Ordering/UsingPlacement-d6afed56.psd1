# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '.LineNumber == lastLine)'
    Replace = '.LineNumber == -1)'
    Tests = 'UsingPlacementTests'
}
