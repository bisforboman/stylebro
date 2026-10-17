# The renamer keeps a base member a derived 'new' member hides (CS0109 otherwise)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& await IsHiddenByNewMemberAsync(solution, symbol, cancellationToken).ConfigureAwait(false))'
    Replace = '&& false)'
    Tests = 'KeptFindingsTests.ABaseMemberHiddenByANewMember_IsKept'
}
