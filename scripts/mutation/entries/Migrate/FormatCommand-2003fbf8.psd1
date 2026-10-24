# Trial 22: verify says what it checks before the progress lines
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (line.StartsWith(ProgressPrefix, StringComparison.Ordinal) || IsHeaderLine(line))'
    Replace = 'if (line.StartsWith(ProgressPrefix, StringComparison.Ordinal))'
    Tests = 'MigrationTests.Verify_IsNotClean_WhenTheCopysCodeFixFailed'
}
