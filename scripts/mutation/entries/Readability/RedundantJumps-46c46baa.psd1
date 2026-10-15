# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '!block.DescendantNodes(n => n == block || !IsFunction(n)).OfType<YieldStatementSyntax>()'
    Replace = '!block.DescendantNodes(n => true).OfType<YieldStatementSyntax>()'
    Tests = 'RedundantJumpTests'
}
