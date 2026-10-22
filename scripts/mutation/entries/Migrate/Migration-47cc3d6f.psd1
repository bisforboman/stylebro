# Trial 45: <Project></Project> on one line gets its closing tag on its own line
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '@"^[ \t]*</Project>") is { Success: true } close'
    Replace = '@"^[ \t]*</Project>") is { Success: false } close'
    Tests = 'MigrationTests.SetPreset'
}
