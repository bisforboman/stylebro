# Trial 31: only fields named <Name>Property
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'var names = field.Name.EndsWith("Property", StringComparison.Ordinal) ?'
    Replace = 'var names = true ?'
    Tests = 'KeptFindingsTests.AFieldRegisteringAXamlProperty'
}
