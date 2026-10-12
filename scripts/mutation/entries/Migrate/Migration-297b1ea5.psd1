# migrate (trial 11, Autofac): SA1309 off lets BRO1306 keep _x names
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'AllowUnderscoreKey} = {Bool(!setup.IsOn("SA1309"))}'
    Replace = 'AllowUnderscoreKey} = {Bool(setup.IsOn("SA1309"))}'
    Tests = 'MigrationTests.PrivateFieldStyle'
}
