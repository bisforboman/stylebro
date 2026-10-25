# Trial 55: '--files' puts a new baseline where a whole run of the repository does (NewBaselineFolder)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var newBaseline = shown is null ? null : NewBaselineFolder(shown);'
    Replace = 'var newBaseline = shown;'
    Tests = 'MigrationTests.Format_Files_PutsANewBaselineWhereAWholeRunOfTheRepositoryDoes'
}
