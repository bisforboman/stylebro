# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'return dynamicNames.Contains(identifier.Identifier.ValueText);'
    Replace = 'return false;'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
