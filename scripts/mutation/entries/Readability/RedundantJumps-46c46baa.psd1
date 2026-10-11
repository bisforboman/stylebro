# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = 'n == block || !IsFunction(n)'
    Replace = 'true'
    Tests = 'RedundantJumpTests'
}
