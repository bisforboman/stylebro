# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'return cast.Type is IdentifierNameSyntax { Identifier.ValueText: "dynamic" };'
    Replace = 'return false;'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
