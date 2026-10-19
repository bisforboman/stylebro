@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'if (Regex.IsMatch(words, @"\bnamespace\b")'
    Replace = 'if (false'
    Tests = 'UsingPlacementTests.Skipped_IfBranchesDeclaringNamespacesOrOuterTypes'
}
