# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (breaking > 0)'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Migrate_FollowsANamingRuleForStatics_TheCodeDoesntBreak'
}
