# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = 'while (first > 0 &&'
    Replace = 'while (false &&'
    Tests = 'RedundantJumpTests'
}
