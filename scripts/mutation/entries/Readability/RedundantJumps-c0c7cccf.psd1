# BRO1137 (redundant return; / yield break;)
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '|| block.Parent is not (BaseMethodDeclarationSyntax'
    Replace = '|| block.Parent is (ArgumentSyntax'
    Tests = 'RedundantJumpTests'
}
