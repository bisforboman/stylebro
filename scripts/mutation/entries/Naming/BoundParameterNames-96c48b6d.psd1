# BRO1302: only Harmony's attributes mark a patch
@{
    File = 'src/StyleBro.Analyzers/Naming/BoundParameterNames.cs'
    Find = 'if (name.StartsWith("Harmony", StringComparison.Ordinal))'
    Replace = 'if (true)'
    Tests = 'CamelCaseNamingTests.ParametersHarmonyBindsByName_AreNotReported'
}
