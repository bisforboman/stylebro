# BRO1141 (object creation parentheses)
@{
    File = 'src/StyleBro.Analyzers/Readability/ObjectCreationParentheses.cs'
    Find = 'if (creation.Initializer is null || '
    Replace = 'if ('
    Tests = 'ObjectCreationParenthesesTests'
}
