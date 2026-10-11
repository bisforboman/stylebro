# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = '|| (conditional.Parent is ConditionalExpressionSyntax parent && parent.WhenFalse == conditional)'
    Replace = ''
    Tests = 'ConditionalLayoutTests'
}
