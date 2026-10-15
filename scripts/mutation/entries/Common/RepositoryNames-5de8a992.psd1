# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'names.UnionWith(variables.Variables.Select(v => v.Identifier.ValueText));'
    Replace = ''
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
