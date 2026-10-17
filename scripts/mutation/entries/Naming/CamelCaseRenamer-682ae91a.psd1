# BRO1301: the 'this.' a renamed local made necessary goes (BRO1131 order, trial 28)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'result.AddRange(await GetUnneededMemberThisAsync(scopeDocument, declaration, location.SourceSpan, current, oldName, cancellationToken).ConfigureAwait(false));'
    Replace = '_ = scopeDocument;'
    Tests = 'CamelCaseNamingTests.AThisALocalMadeNecessary_GoesWithTheRename'
}
