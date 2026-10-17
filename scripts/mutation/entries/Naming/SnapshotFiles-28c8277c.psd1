# Trial 27: element names of the project's own types count.
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'AddWords(found.XamlMarkup, tagName, tag.Groups["tag"].Index);'
    Replace = ''
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
