# Trial 14 (Bogus): a second init keeps a nested .editorconfig's whitespace decision
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'else if (KeptLines(PreviousWhitespace(File.Exists(path) ? File.ReadAllText(path) : null), keys) is { Count: > 0 } kept)'
    Replace = 'else if (KeptLines(PreviousWhitespace(null), keys) is { Count: > 0 } kept)'
    Tests = 'MigrationTests.Init_WritesAWhitespaceDecisionInTheEditorConfigThatDecidesIt_AndKeepsIt'
}
