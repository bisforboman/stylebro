# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'if (style is null || PrivateStaticFields(setup, root, readOnly: true).Any(name => !Regex.IsMatch(name, pattern)))'
    Replace = 'if (style is null)'
    Tests = 'MigrationTests.Migrate_FollowsANamingRuleForStaticReadOnlyFields_TheCodeDoesntBreak'
}
