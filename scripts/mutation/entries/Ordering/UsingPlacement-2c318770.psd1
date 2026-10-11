# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '&& !IsDirective(lines[startLine - 1])'
    Replace = ''
    Tests = 'UsingPlacementTests'
}
