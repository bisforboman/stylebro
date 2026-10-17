# BRO1305: type parameters in crefs are renamed too (trial 28)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'foreach (var (document, span) in await GetCrefTypeParametersAsync(solution, renamedTypeParameter, newName, cancellationToken).ConfigureAwait(false))'
    Replace = 'foreach (var (document, span) in new List<(Document, TextSpan)>())'
    Tests = 'PrefixNamingTests.TypeParametersInCrefs_AreRenamedToo'
}
