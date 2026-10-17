# Only attribute names of the project's own namespaces count.
@{
    File = 'src/StyleBro.CodeFixes/Naming/SnapshotFiles.cs'
    Find = '(attributePrefix.Length > 0 ? local.Contains(attributePrefix) : ownElement || (attributeName.IndexOf(''.'') >= 0 && local.Contains(string.Empty)))'
    Replace = 'true'
    Tests = 'KeptFindingsTests.ANameInAXamlFilesElementOrAttributeName_IsKept'
}
