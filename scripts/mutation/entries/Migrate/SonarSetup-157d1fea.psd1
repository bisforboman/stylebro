# Trial 9: a rule the Sonar version lacks is off
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'foreach (var id in severities.Keys.Where(id => !Exists(id, known)).ToList())'
    Replace = 'foreach (var id in Array.Empty<string>())'
    Tests = 'MigrationTests.SonarRule_SetExplicitly'
}
