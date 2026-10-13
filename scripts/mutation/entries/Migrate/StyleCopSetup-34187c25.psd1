# Trial 15 (Nerdbank.Streams): with several projects MSBuild decides for every one.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var check = uses.Count > 1 || !uses.Values.Contains(true)'
    Replace = 'var check = !uses.Values.Contains(true)'
    Tests = 'FullyQualifiedName~MigrationTests.WithSeveralProjects_MSBuildDecidesForEveryOne'
}
