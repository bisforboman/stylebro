# Trial 50: the Shared files line names the additional-file reason
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return additional is { Count: > 0 }'
    Replace = 'return additional is { Count: > 99 }'
    Tests = 'MigrationTests.Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart'
}
