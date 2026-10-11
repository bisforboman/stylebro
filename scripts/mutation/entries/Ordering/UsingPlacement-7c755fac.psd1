# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '|| root.Members.Count != 1 ||'
    Replace = '|| root.Members.Count < 1 ||'
    Tests = 'UsingPlacementTests'
}
