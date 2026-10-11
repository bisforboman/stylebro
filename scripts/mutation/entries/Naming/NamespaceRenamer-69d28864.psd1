# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if (!generated && root'
    Replace = 'if (root'
    Tests = 'NamespaceNamingTests'
}
