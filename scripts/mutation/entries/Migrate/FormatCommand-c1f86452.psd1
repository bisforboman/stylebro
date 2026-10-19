# Trial 33: xunit.analyzers below 1.24 keeps its fixes.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(v => v >= XunitFixAllEnds)'
    Replace = ''
    Tests = 'MigrationTests.Format_LeavesTheXunitFixesOut'
}
