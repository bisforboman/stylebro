# BRO1107/BRO1108: 'new Action(() => { ... })' is a leading lambda (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = '|| (expression is ObjectCreationExpressionSyntax'
    Replace = '|| (false && expression is ObjectCreationExpressionSyntax'
    Tests = 'ParameterLayoutTests.LeadingMultiLineLambda_StaysWithTheOption'
}
