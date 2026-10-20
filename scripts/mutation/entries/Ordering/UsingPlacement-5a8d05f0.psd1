@{
    File = 'src/StyleBro.Analyzers/Ordering/UsingPlacement.cs'
    Find = 'Regex.IsMatch(words, @"\b(?:class|struct|interface|enum|record|delegate)\b")'
    Replace = 'false'
    Tests = 'UsingPlacementTests.Skipped_IfBranchesDeclaringNamespacesOrOuterTypes'
}
