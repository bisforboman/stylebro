# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '|| statement.DescendantTrivia().Any('
    Replace = '|| statement.DescendantTrivia().All('
    Tests = 'RedundantJumpTests'
}
