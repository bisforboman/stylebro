# Trial 62: a one-letter prefix ('m_count') isn't snake_case.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& name.IndexOf(''_'') > 1)'
    Replace = ')'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountMultiWordPrivateFieldsCamelAndSnakeCase'
}
