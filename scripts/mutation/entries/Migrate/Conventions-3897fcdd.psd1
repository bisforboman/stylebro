# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'ranks.Count(r => r <= 3) >= 2 && ranks.All(r => r <= 3) ? DocOrderStubKey : null'
    Replace = 'null'
    Tests = 'MigrationTests.Conventions_DocOrderOfStubElementsOnly'
}
