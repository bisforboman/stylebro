# preview (trial 11): one sample per hunk, naming every rule
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'b.File == file && b.Hunk.SequenceEqual(lines)'
    Replace = 'false'
    Tests = 'MigrationTests.Preview_OneHunkForSeveralRules'
}
