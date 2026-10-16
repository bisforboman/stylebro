# Trial 24: a lone CR inside a string literal
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (literals.Any(s => s.Contains(at)))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Format_LeavesAStringsLoneCarriageReturn'
}
