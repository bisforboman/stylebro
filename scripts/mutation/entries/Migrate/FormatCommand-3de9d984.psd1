# Trial 55: a plain run's new baseline goes where NewBaselineFolder says, as the preview's
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var newBaseline = shown is null ? null : NewBaselineFolder(shown);'
    Replace = 'var newBaseline = shown;'
    Tests = 'MigrationTests.Format_OnTheRoot_PutsANewBaselineNextToTheOnlySolutionOneFolderDown'
}
