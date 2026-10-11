# BRO1141 (object creation parentheses)
@{
    File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'
    Find = 'return include ? new TextChange'
    Replace = 'return false ? new TextChange'
    Tests = 'ObjectCreationParenthesesTests'
}
