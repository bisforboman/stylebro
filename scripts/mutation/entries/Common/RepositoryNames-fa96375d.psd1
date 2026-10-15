# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'names.Add(parameter.Identifier.ValueText);'
    Replace = ''
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
