# Trial 27: path markup holds no names (only a value that is one name counts).
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '&& NameValue.IsMatch(value));'
    Replace = ');'
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
