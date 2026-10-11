# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '&& !usings.IntersectsWith(t.Span))'
    Replace = ')'
    Tests = 'UsingPlacementTests'
}
