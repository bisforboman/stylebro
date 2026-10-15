# Trial 22: the protected lines get their # back
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Write(file, text.Replace(mark, ''#''), bom);'
    Replace = '_ = bom;'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
