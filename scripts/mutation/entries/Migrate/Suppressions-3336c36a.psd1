# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = 'SST\d{4}|S\d{3,4}'
    Replace = 'S\d{3,4}'
    Tests = 'MigrationTests.StyleSharp_SuppressionsCarryOver_'
}
