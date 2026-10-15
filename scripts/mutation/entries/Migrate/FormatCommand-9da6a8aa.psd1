# Trial 23: a CR inside a string literal isn't an old Mac line ending
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!literals.Any(s => s.Contains(at)))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Format_OldMacLineEndings_NotACarriageReturnInAString'
}
