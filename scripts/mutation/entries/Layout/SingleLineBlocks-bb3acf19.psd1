# BRO1509: a switch with a comment is left alone
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '@switch.DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia))'
    Replace = 'false'
    Tests = 'SingleLineBlocksTests.Switch_GetsItsLines_WhenSingleLineStatementsArentPreserved'
}
