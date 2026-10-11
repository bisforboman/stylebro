# BRO1141 (object creation parentheses)
@{
    File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'
    Find = '|| !Trivia.IsBlank(creation, span)'
    Replace = ''
    Tests = 'ObjectCreationParenthesesTests'
}
