# Trial 26: only attribute values count, not element or attribute names
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'foreach (Match match in Word.Matches(value.Value))'
    Replace = 'foreach (Match match in Word.Matches(attribute.Value))'
    Tests = 'KeptFindingsTests.APropertyInAXamlFile_IsKept'
}
