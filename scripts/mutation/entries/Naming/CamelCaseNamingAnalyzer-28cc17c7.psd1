# BRO1313 (parameter names like the base)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = '|| member is IMethodSymbol { PartialDefinitionPart: not null } or IMethodSymbol { PartialImplementationPart: not null }
            || (!member.IsOverride'
    Replace = '|| (!member.IsOverride'
    Tests = 'ParameterMatchesBaseTests'
}
