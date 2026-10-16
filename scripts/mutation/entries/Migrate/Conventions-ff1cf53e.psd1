# One-line properties decide BRO1509 when they keep one-line bodies
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& !KeepsOneLine(counts[OneLinePropertiesKey])'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountOneLineBlocksWithCode'
}
