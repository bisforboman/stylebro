# doc comments with a blank line below are counted (trial 20, MoreLINQ)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(DocElementBlankLineKey, true);'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountDocBlankLines_PrimaryParameters'
}
