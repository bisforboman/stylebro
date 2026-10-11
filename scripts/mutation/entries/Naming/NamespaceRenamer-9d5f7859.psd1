# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if (root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && partInText.IsMatch(t.ToString())))'
    Replace = 'if (false)'
    Tests = 'NamespaceNamingTests'
}
