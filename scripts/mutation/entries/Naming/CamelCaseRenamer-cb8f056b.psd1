# Trial 21: static properties only by reflection-shaped strings
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'symbol is IFieldSymbol or IPropertySymbol or IEventSymbol && !isData'
    Replace = 'symbol is IFieldSymbol && !isData'
    Tests = 'KeptFindingsTests.AStaticProperty_IsKeptOnlyForAReflectionShapedString'
}
