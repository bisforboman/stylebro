# preview (trial 11, Fitnet): a failed git command stops the preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (code == 0)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Preview_AFailedGitCommand'
}
