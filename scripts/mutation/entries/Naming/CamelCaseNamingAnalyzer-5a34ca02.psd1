# BRO1313 (parameter names like the base)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = 'if (result is not null && result != name)'
    Replace = 'if (false)'
    Tests = 'ParameterMatchesBaseTests'
}
