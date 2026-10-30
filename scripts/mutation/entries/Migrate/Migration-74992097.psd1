# Trial 62: '_shared_count' and '_lastValue' count without their '_'.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var name = variable.Identifier.ValueText.TrimStart(''_'');'
    Replace = 'var name = variable.Identifier.ValueText;'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountMultiWordPrivateFieldsCamelAndSnakeCase'
}
