# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '|| block.Statements.Last() != statement'
    Replace = ''
    Tests = 'RedundantJumpTests'
}
