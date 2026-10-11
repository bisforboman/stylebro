# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if ((parentName.Length == 0 ? global : NamespaceNames.Find(global, parentName))?.GetMembers(newPart).Any() == true)'
    Replace = 'if (false)'
    Tests = 'NamespaceNamingTests'
}
