# Trial 23: a dynamic use is listed with where it is
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'ReflectionKind or DynamicKind ?'
    Replace = 'ReflectionKind ?'
    Tests = 'KeptFindingsTests.RepositoryNames_ListDynamicMemberNames'
}
