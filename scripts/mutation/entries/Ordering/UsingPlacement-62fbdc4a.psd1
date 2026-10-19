@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = '            || DeclaresUnderIf(root, ns))'
    Replace = '            )'
    Tests = 'UsingPlacementTests.Skipped_IfBranchesDeclaringNamespacesOrOuterTypes'
}
