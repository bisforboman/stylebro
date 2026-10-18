# bro migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'optionOnly && !ruleOn(rule)'
    Replace = 'false'
    Tests = 'MigrationTests.Sonar_'
}
