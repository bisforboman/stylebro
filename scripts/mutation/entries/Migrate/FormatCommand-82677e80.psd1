# Trial 24: only where end_of_line is set
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': EndOfLineSet(root, files).Select'
    Replace = ': files.Select'
    Tests = 'MigrationTests.Format_LeavesAStringsLoneCarriageReturn'
}
