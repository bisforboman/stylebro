# Trial 62: init counts snake_case private fields.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '            counts[SnakeFieldKey][1] += snake;'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountMultiWordPrivateFields'
}
