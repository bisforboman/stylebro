# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = ' || InitCommand.EnforcedByIdeRule(convention.Key, repository))'
    Replace = ')'
    Tests = 'MigrationTests.Init_AnIdeRulesOwnSeverity'
}
