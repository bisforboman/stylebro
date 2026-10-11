# BRO1405-BRO1407 (parentheses)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = '!node.IsPartOfStructuredTrivia() && '
    Replace = ''
    Tests = 'ParenthesesTests'
}
