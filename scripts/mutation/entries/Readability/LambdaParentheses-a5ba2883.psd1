# BRO1136 (lambda parentheses)
@{
    File = 'src/StyleBro.Analyzers/Readability/LambdaParentheses.cs'
    Find = 'if (lambda.ParameterList.Parameters.Count != 1'
    Replace = 'if (lambda.ParameterList.Parameters.Count < 1'
    Tests = 'LambdaParenthesesTests'
}
