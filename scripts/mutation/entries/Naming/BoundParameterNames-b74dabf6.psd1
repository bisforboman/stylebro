# BRO1302: Prefix/Postfix are patch methods only in a Harmony-attributed type
@{
    File = 'src/StyleBro.Analyzers/Naming/BoundParameterNames.cs'
    Find = '&& HasHarmonyAttribute(type.AttributeLists);'
    Replace = ';'
    Tests = 'CamelCaseNamingTests.ParametersHarmonyBindsByName_AreNotReported'
}
