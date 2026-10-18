# Trial 31: only static registration fields
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '        if (!field.IsStatic)
        {
            return false;
        }

        var names'
    Replace = '        var names'
    Tests = 'KeptFindingsTests.AFieldRegisteringAXamlProperty'
}
