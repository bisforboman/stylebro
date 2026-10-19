# Trial 33: a CR inside a literal stays.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!literals.Any(s => s.Contains(at)))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_ConvertsOldMacLineEndings'
}
