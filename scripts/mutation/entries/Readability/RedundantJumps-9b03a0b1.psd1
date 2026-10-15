@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = 'block.Statements.Count == 1'
    Replace = 'false'
    Tests = 'RedundantJumpTests.LambdaWhoseEndWouldBeUnreachable'
}
