# BRO1313 (parameter names like the base)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| !CamelCaseNames.IsUsableName(newName)'
    Replace = ''
    Tests = 'ParameterMatchesBaseTests'
}
