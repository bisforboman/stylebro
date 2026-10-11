# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if (NamespaceNames.Find(global, oldFullName) is { } ns && !NamespaceNames.IsOnlyFrom(ns, a => assemblies.Contains(a.Name)))'
    Replace = 'if (false)'
    Tests = 'NamespaceNamingTests'
}
