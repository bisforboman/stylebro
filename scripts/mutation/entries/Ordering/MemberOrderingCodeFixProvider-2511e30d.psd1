# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = '= await ApplyLayoutAsync(document, root, new HashSet<SyntaxNode>(partialAccess.Keys), cancellationToken).ConfigureAwait(false);'
    Replace = '= (document, root, partialAccess.Keys.ToDictionary(k => k, k => k));'
    Tests = 'FixOrderTests.Regions_RemovedAroundMembersMissingBlankLines'
}
