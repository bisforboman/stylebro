# Format speed (2026-10-14): an error is no clean check
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(r.Code == 0 || (r.Code == NotCleanExitCode && kept'
    Replace = '(r.Code == 0 || (r.Code != 0 && kept'
    Tests = 'MigrationTests.Format_ACheckWithOnlyTheKeptFindings_IsClean'
}
