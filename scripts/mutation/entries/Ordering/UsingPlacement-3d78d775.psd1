# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '&& !g.Extensions(name).Concat('
    Replace = '&& false && !g.Extensions(name).Concat('
    Tests = 'UsingPlacementTests'
}
