# BRO1302: a [HarmonyArgument] parameter marks a patch method
@{
    File = 'src/StyleBro.Analyzers/Naming/BoundParameterNames.cs'
    Find = '            if (HasHarmonyAttribute(parameter.AttributeLists))'
    Replace = '            if (false)'
    Tests = 'CamelCaseNamingTests.ParametersHarmonyBindsByName_AreNotReported'
}
