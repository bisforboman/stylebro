# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = '|| access.Name != name)'
    Replace = ')'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
