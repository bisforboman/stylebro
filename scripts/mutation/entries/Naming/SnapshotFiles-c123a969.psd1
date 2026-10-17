# XAML text for people names nothing (trial 28, Text-Grab)
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '|| (!FreeText.Contains(local.Substring(local.LastIndexOf(''.'') + 1)) && NameValue.IsMatch(value));'
    Replace = '|| NameValue.IsMatch(value);'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
