# XAML element and attribute names keep the names they use
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'return names.Files.XamlMarkup.TryGetValue(symbol.Name, out var markup) ? (KeptReason.NameInXamlMarkup, markup)'
    Replace = 'return false && names.Files.XamlMarkup.TryGetValue(symbol.Name, out var markup) ? (KeptReason.NameInXamlMarkup, markup)'
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
