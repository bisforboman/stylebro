# Trial 27: path markup holds no names.
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '&& !(Geometry.Contains('
    Replace = '&& !(false && Geometry.Contains('
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
