# Trial 62: one-word names fit both forms and don't count.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'else if (Regex.IsMatch(name, "^[a-z][a-z0-9]*[A-Z][A-Za-z0-9]*$"))'
    Replace = 'else if (Regex.IsMatch(name, "^[a-z][A-Za-z0-9]*$"))'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountMultiWordPrivateFieldsCamelAndSnakeCase'
}
