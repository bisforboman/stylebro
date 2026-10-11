# BRO1405-BRO1407 (parentheses)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'ParsesTheSame(accepted.Append(node).ToList(), text)'
    Replace = 'true'
    Tests = 'ParenthesesTests'
}
