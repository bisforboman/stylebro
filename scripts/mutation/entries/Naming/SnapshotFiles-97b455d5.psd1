# XAML: the 'x' of '{x:Static ...}' isn't a name
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'AddWords(found.Xaml, Prefix.Replace(value.Value, m => new string('' '', m.Length)), value.Index);'
    Replace = 'AddWords(found.Xaml, value.Value, value.Index);'
    Tests = 'KeptFindingsTests.APrefixInAXamlMarkupExtension_KeepsNoName'
}
