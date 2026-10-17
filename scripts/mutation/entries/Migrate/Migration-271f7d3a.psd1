# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(?:yyyy|year|name|owner|holder|author)'
    Replace = '(?:zzzz)'
    Tests = 'MigrationTests.CopyrightHolder_IgnoresTemplatePlaceholdersAndProse'
}
