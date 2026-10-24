# Trial 50: an additional file another project compiles
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return owners.Count > 0 && loaded.Any('
    Replace = 'return owners.Count < 0 && loaded.Any('
    Tests = 'MigrationTests.Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart'
}
