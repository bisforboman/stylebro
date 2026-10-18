# bro migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '.Where(f => !Path.GetFileName(f).Contains("none", StringComparison.OrdinalIgnoreCase))'
    Replace = ''
    Tests = 'MigrationTests.Sonar_'
}
