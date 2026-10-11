# BRO1313 (parameter names like the base)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| node.Identifier.ValueText.Trim(''_'').Length == 0'
    Replace = ''
    Tests = 'ParameterMatchesBaseTests'
}
