# Trial 27 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var style = InitCommand.NamingRulesOff(own) ? null :'
    Replace = 'var style ='
    Tests = 'MigrationTests.Migrate_FollowsANamingRuleForStatics_TheCodeDoesntBreak'
}
