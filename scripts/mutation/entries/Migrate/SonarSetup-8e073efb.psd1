# stylebro-migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'severities[id] = kind.Max('
    Replace = 'severities[id] = kind.Min('
    Tests = 'MigrationTests.Sonar_'
}
