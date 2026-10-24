# Trial 53: no note when no file has the other form
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (others == 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_AndMigrate_SayWhichFilesTheirCharsetAndFinalNewlineChange'
}
