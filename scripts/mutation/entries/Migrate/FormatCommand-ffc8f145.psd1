# Trial 50: a project's own additional files don't count
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'set.Any(o => !o.Equals(p, StringComparison.OrdinalIgnoreCase))'
    Replace = 'set.Count > 0'
    Tests = 'MigrationTests.Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart'
}
