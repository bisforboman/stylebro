# A props file above the git repository is not imported
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(top is null || folder.Length >= top.Length)'
    Replace = 'true'
    Tests = 'MigrationTests.HasParentProps_StopsAtTheRepositoryRoot'
}
