# Trial 23: member names used through dynamic
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'receiver = element.Expression;'
    Replace = 'return false;'
    Tests = 'KeptFindingsTests.AFieldUsedThroughDynamic_IsKept'
}
