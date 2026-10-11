# stylebro-migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '(string?)r.Element("repositoryKey") == "csharpsquid"'
    Replace = 'true'
    Tests = 'MigrationTests.Sonar_'
}
