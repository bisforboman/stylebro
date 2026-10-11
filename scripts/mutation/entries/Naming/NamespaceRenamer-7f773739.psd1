# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'else if (text == newPart && IsLookedUp(name) && SeesMembersOf(model, name, parentName)'
    Replace = 'else if (false'
    Tests = 'NamespaceNamingTests'
}
