# Trial 50: an additional file another project compiles
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'additionalFiles.Count > 0 && loaded.SelectMany(compile)'
    Replace = 'additionalFiles.Count < 0 && loaded.SelectMany(compile)'
    Tests = 'MigrationTests.Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart'
}
