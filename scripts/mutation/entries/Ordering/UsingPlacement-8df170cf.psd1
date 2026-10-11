# BRO1008 (using placement)
@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'return RegionsOnly(root, checkedEnd, moved, next.Start) ? changes : null;'
    Replace = 'return changes;'
    Tests = 'UsingPlacementTests'
}
