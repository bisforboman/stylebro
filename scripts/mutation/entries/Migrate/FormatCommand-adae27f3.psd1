# Trial 50: no run loads a file as an additional file and a compiled one
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(additional is null || !LoadsAdditionalAsCompile([.. g.Loaded.Union(loaded, StringComparer.OrdinalIgnoreCase)], compile, additional))'
    Replace = 'true'
    Tests = 'MigrationTests.Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart'
}
