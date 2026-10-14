# primary-constructor parameters are counted (trial 20, Altinn)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(PrimaryParametersKey, first == ''_'');'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountDocBlankLines_PrimaryParameters'
}
