# Trial 22: the protected lines lose their # while dotnet format runs
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'offsets.ForEach(o => chars[o] = mark);'
    Replace = '_ = mark;'
    Tests = 'MigrationTests.Format_ProtectsStringLines'
}
