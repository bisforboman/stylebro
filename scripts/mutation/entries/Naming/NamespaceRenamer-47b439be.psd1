# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if (IsLookedUp(name) && !model.LookupSymbols(name.SpanStart, name: newPart).IsEmpty)'
    Replace = 'if (false)'
    Tests = 'NamespaceNamingTests'
}
