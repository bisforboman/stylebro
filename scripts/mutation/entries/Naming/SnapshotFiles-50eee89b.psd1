# Trial 26: only attribute values count, not element or attribute names
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'AddWords(found.Xaml, Prefix.Replace(value.Value, m => new string('' '', m.Length)), value.Index);'
    Replace = 'AddWords(found.Xaml, attribute.Value, attribute.Index);'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
