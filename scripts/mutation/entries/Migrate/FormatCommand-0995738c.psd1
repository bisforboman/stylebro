# Trial 22: the protected lines get their # back
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Write(file, text.Replace(mark, ''#''));'
    Replace = '_ = mark;'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
