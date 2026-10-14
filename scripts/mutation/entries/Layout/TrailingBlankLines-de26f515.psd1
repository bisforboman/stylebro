# Trial 21: BRO1506 leaves standalone comments (owner's choice)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = 'if (comment.Token.IsKind(SyntaxKind.CloseBraceToken))'
    Replace = 'if (false)'
    Tests = 'TrailingBlankLinesTests.StandaloneComments'
}
