# A re-run doesn't keep the CSharpier using lines a second time.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.Where(l => csharpier.Lines?.Contains(l) != true).ToList();'
    Replace = '.ToList();'
    Tests = 'MigrationTests.Init_WithCSharpier'
}
