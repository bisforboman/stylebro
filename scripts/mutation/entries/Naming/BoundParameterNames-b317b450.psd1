# BRO1302: a Harmony patch method's parameters keep their names (trial 29)
@{
    File = 'src/StyleBro.Analyzers/Naming/BoundParameterNames.cs'
    Find = '|| (parameter.Parent?.Parent is MethodDeclarationSyntax method && IsHarmonyPatch(method));'
    Replace = ';'
    Tests = 'CamelCaseNamingTests.ParametersHarmonyBindsByName_AreNotReported'
}
