# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'receiver = parenthesized.Expression;'
    Replace = 'return false;'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
