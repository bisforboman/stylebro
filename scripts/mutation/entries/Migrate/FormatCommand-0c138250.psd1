# Trial 22: end_of_line = unset sets no ending
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'value.Trim().ToLowerInvariant() is "lf" or "crlf" or "cr")'
    Replace = 'true)'
    Tests = 'MigrationTests.Format_GivesFilesTheirEndingBack'
}
