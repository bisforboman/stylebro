# Trial 26: namespace declarations in XAML don't count
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'if (!name.StartsWith("xmlns", StringComparison.Ordinal)'
    Replace = 'if (true'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
