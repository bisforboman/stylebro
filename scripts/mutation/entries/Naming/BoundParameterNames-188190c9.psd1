# BRO1302: a '__' parameter name is Harmony's (trial 29)
@{
    File = 'src/StyleBro.Analyzers/Naming/BoundParameterNames.cs'
    Find = 'return parameter.Identifier.ValueText.StartsWith("__", StringComparison.Ordinal)'
    Replace = 'return false'
    Tests = 'CamelCaseNamingTests.ParametersHarmonyBindsByName_AreNotReported'
}
