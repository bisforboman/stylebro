# the names the renamers check (RepositoryNames, shared with format's per-framework runs)
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'else if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"'
    Replace = 'else if (false'
    Tests = 'InternalTypeMethodTests.NamesInStringsOrNameof'
}
