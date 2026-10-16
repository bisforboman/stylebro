# Only doc comments that mix the stub's elements with later ones count for BRO1619
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'ranks.Any(r => r <= 3) && ranks.Any(r => r >= 4)'
    Replace = 'ranks.Count > 1'
    Tests = 'MigrationTests.Conventions_CountTheDocElementOrder'
}
