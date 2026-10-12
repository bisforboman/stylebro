# BRO1302: parameters of a lambda converted to an expression tree keep their names
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| (GetLambda(node) is { } lambda &&'
    Replace = '|| (false && GetLambda(node) is { } lambda &&'
    Tests = 'CamelCaseNamingTests'
}
