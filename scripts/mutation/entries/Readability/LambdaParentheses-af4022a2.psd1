# BRO1136 (lambda parentheses)
@{
    File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'
    Find = '.Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))'
    Replace = '.Any(t => false))'
    Tests = 'LambdaParenthesesTests'
}
