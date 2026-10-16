# BRO1107/BRO1108: a leading lambda in parentheses is a leading lambda (BRO1405 removes them; trial 25)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = 'expression = parenthesized.Expression;'
    Replace = 'break;'
    Tests = 'FixOrderTests.LeadingMultiLineLambda_InParentheses'
}
