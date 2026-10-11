# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'if (before || after || '
    Replace = 'if (after || '
    Tests = 'ConditionalLayoutTests'
}
