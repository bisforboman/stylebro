# Trial 14 (Bogus): the nearest .editorconfig that sets a whitespace key decides it
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'chain.FirstOrDefault(c => c.Options.ContainsKey(key))'
    Replace = 'chain.LastOrDefault(c => c.Options.ContainsKey(key))'
    Tests = 'MigrationTests.FileSettings_TheNearestEditorConfigThatSetsAKeyDecides'
}
