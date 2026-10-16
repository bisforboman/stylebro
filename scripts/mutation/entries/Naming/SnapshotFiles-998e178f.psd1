# Trial 26: Avalonia .axaml files
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);'
    Replace = 'path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
