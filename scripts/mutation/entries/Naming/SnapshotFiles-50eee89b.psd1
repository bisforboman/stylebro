# Trial 26: only attribute values count, not element or attribute names
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'AddWords(value.Value, value.Index);'
    Replace = 'AddWords(attribute.Value, attribute.Index);'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
