# a nested .editorconfig is judged with the settings that reach it from above
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'var settings = new Dictionary<string, string>(rootSettings, StringComparer.OrdinalIgnoreCase);'
    Replace = 'var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);'
    Tests = 'MigrationTests.NestedHiddenFromFormat'
}
