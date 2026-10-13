# A re-run doesn't keep the CSharpier using lines a second time.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var usings = csharpier.Lines is not null ? [] : KeptLines('
    Replace = 'var usings = false ? [] : KeptLines('
    Tests = 'MigrationTests.Init_WithCSharpier'
}
