# A scope's Sonar severities decide the fixing rules there.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'main.SonarSetup?.For(scope)'
    Replace = 'main.SonarSetup'
    Tests = 'MigrationTests.Sonar_A'
}
