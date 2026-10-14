# only lower-case and '_' names count
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& (first == ''_'' || char.IsLower(first))'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountDocBlankLines_PrimaryParameters'
}
