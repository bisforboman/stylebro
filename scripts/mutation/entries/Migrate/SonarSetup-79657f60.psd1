# Trial 9: Sonar way per version
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'since <= version);'
    Replace = 'true);'
    Tests = 'MigrationTests.SonarDefaults_'
}
