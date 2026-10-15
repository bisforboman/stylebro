@{
    File = 'src/StyleBro.Analyzers/Readability/RedundantJumps.cs'
    Find = '|| block.Statements[block.Statements.Count - 2] is ExpressionStatementSyntax or LocalDeclarationStatementSyntax'
    Replace = ''
    Tests = 'RedundantJumpTests.LambdaWhoseEndWouldBeUnreachable'
}
