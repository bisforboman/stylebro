# tuple element names are counted (trial 20, Altinn)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(StyleBro.Analyzers.Naming.TupleElementNames.CasingKey, char.IsLower(letter));'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountDocBlankLines_PrimaryParameters'
}
