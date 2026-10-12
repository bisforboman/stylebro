# A fixing rule the main lines turn on and a scope does not is turned off there.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'key => fixingSonar.Contains(key) &&'
    Replace = 'key => false &&'
    Tests = 'MigrationTests.Init_SonarSeveritiesPerFolder'
}
