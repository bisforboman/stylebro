# stylebro-migrate follows a SonarQube setup (2026-10-09)
@{
    File = 'src/StyleBro.Migrate/SonarSetup.cs'
    Find = '(string?)r.Attribute("AnalyzerId") == "SonarAnalyzer.CSharp"'
    Replace = 'true'
    Tests = 'MigrationTests.Sonar_'
}
