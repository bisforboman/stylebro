# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = '|| conditional.WhenFalse is ConditionalExpressionSyntax'
    Replace = ''
    Tests = 'ConditionalLayoutTests'
}
