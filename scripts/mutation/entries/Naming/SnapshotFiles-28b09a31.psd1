# A plain attribute on the project's own element names its property.
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = 'ownElement || (attributeName'
    Replace = 'false || (attributeName'
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
