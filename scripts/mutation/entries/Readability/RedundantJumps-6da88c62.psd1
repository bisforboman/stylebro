# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '.OfType<YieldStatementSyntax>().Skip(1).Any()'
    Replace = '.OfType<YieldStatementSyntax>().Any()'
    Tests = 'RedundantJumpTests'
}
