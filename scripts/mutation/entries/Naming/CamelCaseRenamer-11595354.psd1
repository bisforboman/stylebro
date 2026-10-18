# Trial 31: fields registering XAML properties keep their names (Ursa.Avalonia)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (symbol is IFieldSymbol field && IsRegistrationField(field))'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests.AFieldRegisteringAXamlProperty'
}
