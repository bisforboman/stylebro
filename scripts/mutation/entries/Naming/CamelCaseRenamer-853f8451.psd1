# Trial 21: static properties are not data
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& !symbol.IsStatic)'
    Replace = ')'
    Tests = 'KeptFindingsTests.AStaticProperty_IsKeptOnlyForAReflectionShapedString'
}
