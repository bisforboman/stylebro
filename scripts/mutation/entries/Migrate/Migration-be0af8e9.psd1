# Trial 43: an .editorconfig 'default' doesn't override a global bulk severity in 'dotnet format'.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '!p.Value.Trim().Equals("default", StringComparison.OrdinalIgnoreCase) || !merged.ContainsKey(p.Key)'
    Replace = 'true'
    Tests = 'MigrationTests.WithGlobalBulk'
}
