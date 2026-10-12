# BRO1122: only the parentheses BRO1405 would remove ((-1L) keeps them)
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = '|| !Maintainability.Parentheses.IsUnnecessary(newParenthesized, newText))'
    Replace = ')'
    Tests = 'LiteralSuffixTests'
}
