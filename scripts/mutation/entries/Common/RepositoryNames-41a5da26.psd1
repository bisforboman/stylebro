# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'if (dynamicNames is not null && IsDynamicMemberName(token, dynamicNames))'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
