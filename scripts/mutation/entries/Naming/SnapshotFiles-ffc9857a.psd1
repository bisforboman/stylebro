# XAML: a value that is one name (an event handler) names code
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '|| (!FreeText.Contains(local.Substring(local.LastIndexOf(''.'') + 1)) && NameValue.IsMatch(value));'
    Replace = ';'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
