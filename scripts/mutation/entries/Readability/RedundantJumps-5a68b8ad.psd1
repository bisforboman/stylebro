# Trial 22: a lambda keeps a return; whose removal makes its end unreachable.
@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '|| (block.Parent is AnonymousFunctionExpressionSyntax && !EndStaysReachable(block, statement)))'
    Replace = ')'
    Tests = 'RedundantJumpTests.LambdaWhoseEndWouldBeUnreachable'
}
