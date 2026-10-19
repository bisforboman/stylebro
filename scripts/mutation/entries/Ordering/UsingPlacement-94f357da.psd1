@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '        return depth > 0;'
    Replace = '        return false;'
    Tests = 'UsingPlacementTests.Skipped_IfBranchesDeclaringNamespacesOrOuterTypes'
}
