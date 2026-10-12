# BRO1313 non_generic_methods: indexers are skipped, like S927.
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '&& (member is not IMethodSymbol'
    Replace = '&& (false'
    Tests = 'ParameterMatchesBaseTests'
}
