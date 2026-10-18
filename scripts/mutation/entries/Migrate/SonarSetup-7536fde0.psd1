# bro migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '(StyleCopSetup.ParseSeverity(ValueOf(lines[existing])) ?? Severity.None) < severity'
    Replace = 'false'
    Tests = 'MigrationTests.Sonar_'
}
