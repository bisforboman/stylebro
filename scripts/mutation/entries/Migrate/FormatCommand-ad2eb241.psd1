# format: files that mixed line endings before the run are left as they are (#133)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(e => e.Crlf is not null)'
    Replace = ''
    Tests = 'FormatLineEndingsTests.Format_LeavesFilesThatMixedEndingsBefore'
}
