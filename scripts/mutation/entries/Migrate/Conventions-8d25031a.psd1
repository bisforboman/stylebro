# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '                                counts[DocOrderTagPrefix + order[before]][0]++;'
    Replace = ''
    Tests = 'MigrationTests.DocOrder_NamesTheElementsOutOfOrder'
}
