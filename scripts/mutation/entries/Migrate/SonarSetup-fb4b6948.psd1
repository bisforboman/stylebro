# Trial 24 (Refit): Sonar ids all off with no package is no Sonar setup
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = 'if (profile is null && version is null && !severities.Values.Any(s => s > Severity.None))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Sonar_AllOffWithoutThePackage_IsNoSonarSetup'
}
